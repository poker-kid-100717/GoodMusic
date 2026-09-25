namespace MyMusic.API.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "MyMusic.API";
    public string Audience { get; set; } = "MyMusic.API";
    public int LifetimeMinutes { get; set; } = 60;
}
