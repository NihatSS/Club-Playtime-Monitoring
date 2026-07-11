namespace ClubPlaytime.Api.Models;

public sealed class PlayerActivityEvent
{
    public int Id { get; set; }

    public int PlayerId { get; set; }

    public Player? Player { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public long DeltaSeconds { get; set; }

    public DateTime OccurredAt { get; set; }
}
