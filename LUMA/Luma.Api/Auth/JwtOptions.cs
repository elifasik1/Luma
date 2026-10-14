namespace Luma.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Luma.Api";
    public string Audience { get; set; } = "Luma.Frontend";
    public int ExpirationMinutes { get; set; } = 60;
}
