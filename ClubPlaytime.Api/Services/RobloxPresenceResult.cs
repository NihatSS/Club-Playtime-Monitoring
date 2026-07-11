namespace ClubPlaytime.Api.Services;

public sealed record RobloxPresenceResult(
    bool IsSuccessful,
    bool SelectorFound,
    bool IsOnline,
    bool IsPlayingTargetGame,
    string? CurrentGame,
    string? ErrorMessage)
{
    public static RobloxPresenceResult Failed(string message)
    {
        return new RobloxPresenceResult(false, false, false, false, null, message);
    }
}
