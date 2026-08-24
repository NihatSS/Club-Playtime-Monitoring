namespace ClubPlaytime.Api.Models;

public sealed class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "User"; // "User" or "Admin"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
