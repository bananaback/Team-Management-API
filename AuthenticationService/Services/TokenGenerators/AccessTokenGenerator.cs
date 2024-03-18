using System.Security.Claims;
using AuthenticationService.Models;

namespace AuthenticationService.Services.TokenGenerators;

public class AccessTokenGenerator : ITokenGenerator
{
    private readonly AuthenticationConfiguration _authenticationConfiguration;
    private readonly TokenGenerator _tokenGenerator;
    public AccessTokenGenerator(AuthenticationConfiguration authenticationConfiguration, TokenGenerator tokenGenerator)
    {
        _authenticationConfiguration = authenticationConfiguration;
        _tokenGenerator = tokenGenerator;
    }

    public string GenerateToken(ApplicationUser user)
    {
        List<Claim> claims = new List<Claim>()
        {
            new Claim("id", user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.UserRole.ToString())
        };

        return _tokenGenerator.GenerateToken(_authenticationConfiguration.AccessTokenSecret,
        _authenticationConfiguration.Issuer,
        _authenticationConfiguration.Audiences,
        _authenticationConfiguration.AccessTokenExpirationMinutes, claims);
    }
}