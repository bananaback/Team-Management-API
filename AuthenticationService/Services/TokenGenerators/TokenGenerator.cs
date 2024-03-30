using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticationService.Services.TokenGenerators;

public class TokenGenerator
{
    public string GenerateToken(string secretKey, string issuer, List<string> audiences, double expirationMinutes, IEnumerable<Claim>? claims = null)
    {
        SecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        claims = claims ?? new List<Claim>();  // Ensure a non-null list
        var claimsList = claims.ToList();

        foreach (string audience in audiences)
        {
            Claim audClaim = new Claim("aud", audience);
            claimsList.Add(audClaim);
        }

        JwtSecurityToken token = new JwtSecurityToken(
            issuer,
            null,
            claimsList,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(expirationMinutes),
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}