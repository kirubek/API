namespace BaseOps.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "BaseOps";
    public string Audience { get; set; } = "BaseOps.Frontend";
    public string SigningKey { get; set; } = "CHANGE_ME_TO_A_64_BYTE_PRODUCTION_SECRET_CHANGE_ME";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
