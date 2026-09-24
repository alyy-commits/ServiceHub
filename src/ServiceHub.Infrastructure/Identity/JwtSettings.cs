namespace ServiceHub.Infrastructure.Identity;

/// <summary>Options POCO bound from the "Jwt" configuration section (public setters are required by the binder).</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
