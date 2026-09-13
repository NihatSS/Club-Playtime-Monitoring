namespace ClubPlaytime.Api.Models;

public sealed class PlayerActivityEvent
{
    public int Id { get; set; }

    public int PlayerId { get; set; }

    public Player? Player { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public long DeltaSeconds { get; set; }

    /// <summary>Game this event refers to (e.g. the target game, or any other Roblox game the player was in).</summary>
    public string? GameName { get; set; }

    /// <summary>Roblox place ID of the game, when known from the Presence API (used to resolve the game icon).</summary>
    public long? PlaceId { get; set; }

    public DateTime OccurredAt { get; set; }
}
