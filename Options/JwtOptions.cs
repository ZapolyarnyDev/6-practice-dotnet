namespace Practice6.Options;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string Secret { get; set; } = "";
    public int AccessMinutes { get; set; } = 15;
}
