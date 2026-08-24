namespace ClubPlaytime.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "ClubPlaytime";

    public string Audience { get; set; } = "ClubPlaytime";

    public int ExpirationMinutes { get; set; } = 1440; // 24 hours
}
