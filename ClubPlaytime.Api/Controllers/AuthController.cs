using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.DTOs;
using ClubPlaytime.Api.Options;
using ClubPlaytime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClubPlaytime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    ClubPlaytimeDbContext dbContext,
    IOptions<JwtOptions> jwtOptions,
    IRobloxProfileClient robloxProfileClient,
    ILogger<AuthController> logger) : ControllerBase
{
    // Unambiguous alphabet (no 0/O, 1/I/L) so codes are easy to type into Roblox.
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const string CodePrefix = "RPT-";
    private const int CodeLength = 5;
    private static readonly TimeSpan VerificationLifetime = TimeSpan.FromMinutes(15);

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var token = GenerateJwtToken(user);

        return Ok(new LoginResponse
        {
            Token = token,
            Role = user.Role,
            Username = user.Username
        });
    }

    /// <summary>
    /// Public registration. Always creates a USER role account (never Admin), so
    /// there is no privilege escalation path through registration. If a
    /// <see cref="RegisterRequest.ClaimToken"/> is supplied (obtained after Roblox
    /// ownership verification), the account is linked to the verified existing
    /// tracker player — no duplicate player is created and playtime is preserved.
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(RegisterRequest request)
    {
        var username = (request.Username ?? string.Empty).Trim();
        var password = request.Password ?? string.Empty;

        if (username.Length < 3 || username.Length > 50)
        {
            return BadRequest(new { message = "Username must be between 3 and 50 characters." });
        }

        if (password.Length < 6)
        {
            return BadRequest(new { message = "Password must be at least 6 characters." });
        }

        if (await dbContext.Users.AnyAsync(u => u.Username == username))
        {
            return Conflict(new { message = $"Username '{username}' is already taken." });
        }

        int? playerId = null;

        if (!string.IsNullOrWhiteSpace(request.ClaimToken))
        {
            var verification = await dbContext.VerificationCodes
                .FirstOrDefaultAsync(v => v.ClaimToken == request.ClaimToken.Trim());

            if (verification is null || verification.ClaimedAt is not null)
            {
                return BadRequest(new { message = "Invalid or already-used claim token. Start the verification again." });
            }

            var player = await dbContext.Players
                .FirstOrDefaultAsync(p => p.RobloxUserId == verification.RobloxUserId);

            if (player is null)
            {
                return BadRequest(new { message = "The verified Roblox player no longer exists in the tracker." });
            }

            // A player can only be claimed by one website account.
            var alreadyClaimed = await dbContext.Users.AnyAsync(u => u.PlayerId == player.Id);
            if (alreadyClaimed)
            {
                return Conflict(new { message = "This tracker player has already been claimed by another account." });
            }

            playerId = player.Id;
            verification.ClaimedAt = DateTime.UtcNow;
        }

        var user = new Models.User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "User",
            PlayerId = playerId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return Ok(new LoginResponse
        {
            Token = token,
            Role = user.Role,
            Username = user.Username
        });
    }

    /// <summary>
    /// Step 1 of claiming an existing tracker player: generates a unique,
    /// single-use verification code tied to the selected Roblox user ID.
    /// </summary>
    [HttpPost("verify/start")]
    public async Task<ActionResult<VerifyStartResponse>> StartVerification(VerifyStartRequest request)
    {
        var player = await dbContext.Players
            .FirstOrDefaultAsync(p => p.RobloxUserId == request.RobloxUserId);

        if (player is null)
        {
            return NotFound(new { message = "This player is not in the tracker." });
        }

        // Only allow claiming players that aren't already linked to a website account.
        var alreadyClaimed = await dbContext.Users.AnyAsync(u => u.PlayerId == player.Id);
        if (alreadyClaimed)
        {
            return Conflict(new { message = "This tracker player has already been claimed by another account." });
        }

        // Invalidate any previous unused codes for this player so old codes can't be reused.
        var now = DateTime.UtcNow;
        var staleCodes = await dbContext.VerificationCodes
            .Where(v => v.RobloxUserId == request.RobloxUserId && v.UsedAt == null && v.ClaimToken == null)
            .ToListAsync();
        foreach (var stale in staleCodes)
        {
            stale.UsedAt = now; // single-use semantics: a fresh code replaces old ones
        }

        var code = GenerateCode();
        var verification = new Models.VerificationCode
        {
            RobloxUserId = request.RobloxUserId,
            Code = code,
            CreatedAt = now,
            ExpiresAt = now.Add(VerificationLifetime)
        };

        dbContext.VerificationCodes.Add(verification);
        await dbContext.SaveChangesAsync();

        return Ok(new VerifyStartResponse
        {
            VerificationId = verification.Id,
            Code = code,
            ExpiresAt = verification.ExpiresAt,
            RobloxUsername = player.Username,
            RobloxUserId = player.RobloxUserId,
            AvatarUrl = player.AvatarUrl,
            Club = player.Club
        });
    }

    /// <summary>
    /// Step 2: checks the user's PUBLIC Roblox profile (by Roblox user ID, never
    /// username) for the generated code. On success issues a single-use claim token
    /// and marks the verification code as used.
    /// </summary>
    [HttpPost("verify/check")]
    public async Task<ActionResult<VerifyCheckResponse>> CheckVerification(VerifyCheckRequest request)
    {
        var verification = await dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.Id == request.VerificationId);

        if (verification is null || verification.ClaimedAt is not null)
        {
            return NotFound(new { message = "Verification not found." });
        }

        if (verification.UsedAt is not null)
        {
            return BadRequest(new { message = "This verification code has already been used. Start a new verification." });
        }

        if (DateTime.UtcNow > verification.ExpiresAt)
        {
            return BadRequest(new { message = "This verification code has expired. Start a new verification." });
        }

        if (!string.Equals(verification.Code, request.Code.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "The code you entered doesn't match. Copy the exact code shown." });
        }

        var player = await dbContext.Players
            .FirstOrDefaultAsync(p => p.RobloxUserId == verification.RobloxUserId);

        if (player is null)
        {
            return BadRequest(new { message = "The verified Roblox player no longer exists in the tracker." });
        }

        var alreadyClaimed = await dbContext.Users.AnyAsync(u => u.PlayerId == player.Id);
        if (alreadyClaimed)
        {
            return Conflict(new { message = "This tracker player has already been claimed by another account." });
        }

        string? description;
        try
        {
            description = await robloxProfileClient.GetDescriptionAsync(verification.RobloxUserId, HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Roblox profile check failed for user {RobloxUserId}", verification.RobloxUserId);
            return Ok(new VerifyCheckResponse
            {
                Verified = false,
                Message = "We couldn't check your Roblox profile right now. Please try again in a moment."
            });
        }

        // Match case-insensitively so minor case differences in the code don't fail.
        var codeFound = description is not null
                        && description.Contains(verification.Code, StringComparison.OrdinalIgnoreCase);

        if (!codeFound)
        {
            return Ok(new VerifyCheckResponse
            {
                Verified = false,
                Message = "We couldn't verify your Roblox account yet. Make sure the code is correctly placed in your Roblox profile 'About' section, then try again."
            });
        }

        // Success: code is single-use from here on.
        var claimToken = GenerateClaimToken();
        verification.UsedAt = DateTime.UtcNow;
        verification.ClaimToken = claimToken;
        await dbContext.SaveChangesAsync();

        return Ok(new VerifyCheckResponse
        {
            Verified = true,
            ClaimToken = claimToken,
            Message = "Roblox account verified. You can now remove the code from your Roblox profile.",
            Player = new ClaimedPlayerDto
            {
                PlayerId = player.Id,
                Username = player.Username,
                RobloxUserId = player.RobloxUserId,
                AvatarUrl = player.AvatarUrl,
                Club = player.Club,
                TotalPlaySeconds = player.TotalPlaySeconds
            }
        });
    }

    /// <summary>
    /// Change your own password (requires the current password).
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var username = User.Identity?.Name;
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user is null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(new { message = "Current password is incorrect." });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await dbContext.SaveChangesAsync();

        return Ok(new { message = "Password updated successfully." });
    }

    /// <summary>
    /// Current authenticated user's profile (role, linked player, discord id).
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var username = User.Identity?.Name;
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user is null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        return Ok(new MeResponse
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            PlayerId = user.PlayerId,
            DiscordUserId = user.DiscordUserId
        });
    }

    private string GenerateJwtToken(Models.User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Value.Issuer,
            audience: jwtOptions.Value.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(CodeLength);
        var chars = new char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            chars[i] = CodeAlphabet[bytes[i] % CodeAlphabet.Length];
        }

        return CodePrefix + new string(chars);
    }

    private static string GenerateClaimToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    }
}