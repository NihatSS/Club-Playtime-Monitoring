namespace ClubPlaytime.Api.Models;

/// <summary>
/// A single-use, expiring code used to prove ownership of a Roblox account.
/// The user places the code in their Roblox profile "About" section and the
/// backend verifies it against the public Roblox users API by Roblox user ID.
/// </summary>
public sealed class VerificationCode
{
    public int Id { get; set; }

    /// <summary>The Roblox user ID the verification is tied to (never username).</summary>
    public long RobloxUserId { get; set; }

    /// <summary>The randomly generated code shown to the user (e.g. RPT-7K29X).</summary>
    public string Code { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    /// <summary>Set when the code is successfully verified — a code is single-use.</summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// Random token issued after successful verification. The website account
    /// registration consumes this token to claim the verified tracker player.
    /// </summary>
    public string? ClaimToken { get; set; }

    /// <summary>Set when the claim token has been consumed by a registration.</summary>
    public DateTime? ClaimedAt { get; set; }
}