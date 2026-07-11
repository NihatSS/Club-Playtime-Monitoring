namespace ClubPlaytime.Api.Models;

public sealed class DailyPlaytime
{
    public int Id { get; set; }

    public int PlayerId { get; set; }

    public Player? Player { get; set; }

    public DateOnly Date { get; set; }

    public long PlaySeconds { get; set; }
}
