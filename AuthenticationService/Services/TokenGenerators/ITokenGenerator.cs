using AuthenticationService.Models;

namespace AuthenticationService.Services.TokenGenerators;

public interface ITokenGenerator
{
    public string GenerateToken(ApplicationUser user);
}