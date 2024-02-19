using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AuthenticationService.Exceptions;
using AuthenticationService.Models;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticationService.Services.TokenValidators;

public class RefreshTokenValidator
{
    private readonly AuthenticationConfiguration _authenticationConfiguration;
    public RefreshTokenValidator(AuthenticationConfiguration authenticationConfiguration)
    {
        _authenticationConfiguration = authenticationConfiguration;
    }

    public RefreshTokenClaims ExtractTokenClaims(string refreshToken)
    {
        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        TokenValidationParameters validationParameters = new TokenValidationParameters()
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authenticationConfiguration.RefreshTokenSecret)),
            ValidIssuer = _authenticationConfiguration.Issuer,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidAudiences = _authenticationConfiguration.Audiences,
            ClockSkew = TimeSpan.Zero,
        };

        var claimsPrincipal = tokenHandler.ValidateToken(refreshToken, validationParameters, out SecurityToken validatedToken);

        var expirationClaim = claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Exp);

        if (expirationClaim is null)
        {
            throw new RequiredTokenClaimNotFoundException("Expiration claim not found");
        }

        var idClaim = claimsPrincipal.FindFirst(c => c.Type == "id");

        if (idClaim is null)
        {
            throw new RequiredTokenClaimNotFoundException("User id claim not found");
        }

        long expirationTimestamp = long.Parse(expirationClaim.Value);
        DateTime expirationDateTime = DateTimeOffset.FromUnixTimeSeconds(expirationTimestamp).UtcDateTime;

        // Calculate remaining time until expiration
        TimeSpan remainingTime = expirationDateTime - DateTime.UtcNow;

        return new RefreshTokenClaims(idClaim.Value, expirationDateTime);
    }
}