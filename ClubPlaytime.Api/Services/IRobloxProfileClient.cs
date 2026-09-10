namespace ClubPlaytime.Api.Services;

/// <summary>
/// Reads public Roblox profile information (the "About" / description) for a
/// given Roblox user ID using the official, unauthenticated Roblox users API:
/// GET https://users.roblox.com/v1/users/{userId} — returns the profile
/// description field. This is how account ownership is verified: the user puts
/// a generated code in their About section and we check it shows up here.
/// </summary>
public interface IRobloxProfileClient
{
    /// <summary>
    /// Returns the user's profile description (About section), or null when the
    /// user does not exist. Transient failures are retried briefly because the
    /// description can take a few seconds to propagate after the user edits it.
    /// </summary>
    Task<string?> GetDescriptionAsync(long robloxUserId, CancellationToken cancellationToken = default);
}