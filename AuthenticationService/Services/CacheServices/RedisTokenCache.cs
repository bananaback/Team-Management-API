using System.IdentityModel.Tokens.Jwt;
using AuthenticationService.Exceptions;
using AuthenticationService.Models;
using AuthenticationService.Services.TokenValidators;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace AuthenticationService.Services.CacheServices;

public class RedisTokenCache
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly AuthenticationConfiguration _authenticationConfiguration;
    private readonly RefreshTokenValidator _refreshTokenValidator;
    public RedisTokenCache(IConnectionMultiplexer connectionMultiplexer, AuthenticationConfiguration authenticationConfiguration, RefreshTokenValidator refreshTokenValidator)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _authenticationConfiguration = authenticationConfiguration;
        _refreshTokenValidator = refreshTokenValidator;
    }

    public async Task<bool> IsRefreshTokenRevoked(string refreshToken)
    {
        IDatabase database = _connectionMultiplexer.GetDatabase();
        return await database.KeyExistsAsync(refreshToken);
    }

    public async Task RevokeRefreshToken(string refreshToken)
    {
        IDatabase database = _connectionMultiplexer.GetDatabase();
        try
        {
            RefreshTokenClaims refreshTokenClaims = _refreshTokenValidator.ExtractTokenClaims(refreshToken);
            TimeSpan remainingTime = refreshTokenClaims.ExpirationDate - DateTime.UtcNow;

            await database.StringSetAsync(refreshToken, "revoked", remainingTime);

            await database.SetRemoveAsync(refreshTokenClaims.UserId, refreshToken);

            Console.WriteLine($"Refresh token '{refreshToken}' has been revoked in Redis.");
        }
        catch (RequiredTokenClaimNotFoundException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token validation failed: {ex.Message}");
        }
    }

    public async Task RevokeAllRefreshTokensOfUser(string userId)
    {
        IDatabase database = _connectionMultiplexer.GetDatabase();
        RedisValue[] setMembers = await database.SetMembersAsync(userId);
        foreach (var member in setMembers)
        {
            await RevokeRefreshToken(member.ToString());
        }
    }

    public async void TrackUserRefreshToken(Guid userId, string refreshToken)
    {
        IDatabase database = _connectionMultiplexer.GetDatabase();
        await database.SetAddAsync(userId.ToString(), refreshToken);
    }
}