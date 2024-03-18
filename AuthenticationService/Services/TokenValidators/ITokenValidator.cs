using AuthenticationService.Models;

namespace AuthenticationService.Services.TokenValidators;

public interface ITokenValidator
{
    public RefreshTokenClaims ExtractTokenClaims(string refreshToken);
}