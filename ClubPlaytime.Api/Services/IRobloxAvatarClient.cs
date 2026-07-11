namespace ClubPlaytime.Api.Services;

public interface IRobloxAvatarClient
{
    Task<string?> GetAvatarUrlAsync(long robloxUserId, CancellationToken cancellationToken = default);
}
