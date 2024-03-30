namespace AuthenticationService.Models;

public class AuthenticationConfiguration
{
    public string AccessTokenSecret { get; set; }
    public string RefreshTokenSecret { get; set; }
    public double AccessTokenExpirationMinutes { get; set; }
    public double RefreshTokenExpirationMinutes { get; set; }
    public string Issuer { get; set; }
    public List<string> Audiences { get; set; }

    public AuthenticationConfiguration()
    {
        AccessTokenSecret = string.Empty;
        RefreshTokenSecret = string.Empty;
        Issuer = string.Empty;
        Audiences = new List<string>();
    }

    public AuthenticationConfiguration(
        string accessTokenSecret,
        string refreshTokenSecret,
        double accessTokenExpirationMinutes,
        double refreshTokenExpirationMinutes,
        string issuers,
        List<string> audiences)
    {
        AccessTokenSecret = accessTokenSecret;
        RefreshTokenSecret = refreshTokenSecret;
        AccessTokenExpirationMinutes = accessTokenExpirationMinutes;
        RefreshTokenExpirationMinutes = refreshTokenExpirationMinutes;
        Issuer = issuers;
        Audiences = audiences;
    }
}