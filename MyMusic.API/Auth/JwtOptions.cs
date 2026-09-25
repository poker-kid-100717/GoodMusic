namespace MyMusic.API.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HS256 requires a signing key of at least 256 bits.</summary>
    public const int MinimumKeyBytes = 32;

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "MyMusic.API";
    public string Audience { get; set; } = "MyMusic.API";
    public int LifetimeMinutes { get; set; } = 60;
}
