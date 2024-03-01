using System.Security.Claims;
using AuthenticationService.Models;

namespace AuthenticationService.Services.TokenGenerators;

public class RefreshTokenGenerator
{
    private readonly AuthenticationConfiguration _authenticationConfiguration;
    private readonly TokenGenerator _tokenGenerator;

    public RefreshTokenGenerator(AuthenticationConfiguration authenticationConfiguration, TokenGenerator tokenGenerator)
    {
        _authenticationConfiguration = authenticationConfiguration;
        _tokenGenerator = tokenGenerator;
    }

    public string GenerateToken(ApplicationUser user)
    {
        List<Claim> claims = new List<Claim>()
        {
            new Claim("id", user.UserId.ToString())
        };
        return _tokenGenerator.GenerateToken(
                _authenticationConfiguration.RefreshTokenSecret,
                _authenticationConfiguration.Issuer,
                _authenticationConfiguration.Audiences,
                _authenticationConfiguration.RefreshTokenExpirationMinutes,
                claims);
    }
}