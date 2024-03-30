using System.ComponentModel;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using AuthenticationService.Exceptions;
using AuthenticationService.Models;
using AuthenticationService.Services.CacheService;
using AuthenticationService.Services.TokenValidators;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace AuthenticationService.Services.CacheServices;

public class RedisTokenCache : ITokenCache
{
    private readonly AuthenticationConfiguration _authenticationConfiguration;
    private readonly ITokenValidator _refreshTokenValidator;
    private readonly Task<RedisConnection> _redisConnectionFactory;
    private readonly ILogger<RedisTokenCache> _logger;
    private RedisConnection? _redisConnection;
    public RedisTokenCache(ILogger<RedisTokenCache> logger, Task<RedisConnection> redisConnectionFactory, AuthenticationConfiguration authenticationConfiguration, ITokenValidator refreshTokenValidator)
    {
        _authenticationConfiguration = authenticationConfiguration;
        _refreshTokenValidator = refreshTokenValidator;
        _redisConnectionFactory = redisConnectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Ensure redis connection is initialized by check if it is null, if so await the redis connection factory to get one, 
    /// if it take too long to obtain the connection, TimeOutException is thrown.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
    private async Task EnsureRedisConnectionInitializedAsync()
    {
        if (_redisConnection == null)
        {
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
            var connectTask = _redisConnectionFactory;
            var combinedTask = await Task.WhenAny(timeoutTask, connectTask);
            if (combinedTask == timeoutTask)
            {
                throw new TimeoutException("Redis connection initialization timed out.");
            }
            else
            {
                _redisConnection = await connectTask;
            }
        }
    }


    /// <summary>
    /// Checks if a refresh token is revoked in Redis.
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    /// <exception cref="TokenCacheException"></exception>
    public async Task<bool> IsRefreshTokenRevoked(string refreshToken)
    {
        try
        {
            await EnsureRedisConnectionInitializedAsync();
            bool isRefreshTokenRevoked = await _redisConnection!.ExecuteWithBackgroundReconnectAsync(async (db) => await db.KeyExistsAsync(refreshToken));
            return isRefreshTokenRevoked;
        }
        catch (TimeoutException timeoutEx)
        {
            _logger.LogError(timeoutEx, $"Check revocation failed for refresh token : {refreshToken}."
                        + "A timeout exception occurs while ensure redis connection initialized: " + timeoutEx.Message);
            throw new TokenCacheException("Failed to check refresh token revocation");
        }
        catch (Exception ex) when (ex is RedisConnectionException || ex is SocketException || ex is ObjectDisposedException)
        {
            _logger.LogError(ex, $"Check revocation failed for refresh token : {refreshToken}."
                        + "A redis-related exception occurs while execute operation: " + ex.Message);
            throw new TokenCacheException("Failed to check refresh token revocation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Check revocation failed for refresh token : {refreshToken}."
                            + "An unexpected exception occurs while checking token revocation: " + ex.Message);
            throw new TokenCacheException("Failed to check refresh token revocation");
        }
    }




    /// <summary>
    /// Revoke a refresh token by adding it to token blacklist in Redis.
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    /// <exception cref="TokenCacheException"></exception>
    public async Task RevokeRefreshToken(string refreshToken)
    {
        try
        {
            RefreshTokenClaims refreshTokenClaims = _refreshTokenValidator.ExtractTokenClaims(refreshToken);
            TimeSpan remainingTime = refreshTokenClaims.ExpirationDate - DateTime.UtcNow;

            await EnsureRedisConnectionInitializedAsync();

            await _redisConnection!.ExecuteWithBackgroundReconnectAsync(async (db) => await db.StringSetAsync(refreshToken, "revoked", remainingTime));

            await _redisConnection!.ExecuteWithBackgroundReconnectAsync(async (db) => await db.SetRemoveAsync(refreshTokenClaims.UserId, refreshToken));

            _logger.LogInformation($"Refresh token '{refreshToken}' has been revoked in Redis.");
        }
        catch (TokenExtractionException ex)
        {
            _logger.LogWarning(ex, $"Token revocation failed. Token is not valid for caching: {refreshToken}" + ex.Message);

            throw new TokenCacheException("Token not valid for caching" + ex.Message);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, $"Token revocation failed for token: {refreshToken}. Caching issue. "
                + "An timeout exception occurs while ensure redis connection initialized: " + ex.Message);

            throw new TokenCacheException("Failed to revoke refresh token: " + ex.Message);
        }
        catch (Exception ex) when (ex is RedisConnectionException || ex is SocketException || ex is ObjectDisposedException)
        {
            _logger.LogError(ex, $"Token revocation failed for token: {refreshToken}. Caching issue. "
                + "An redis-related exception occurs while execute operation: " + ex.Message);

            throw new TokenCacheException("Failed to revoke refresh token: " + ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Token revocation failed for token: {refreshToken}. Caching issue. "
                + $"Token validation failed due to unexpected exceptions: {ex.Message}");

            throw new TokenCacheException("Failed to revoke refresh token: " + ex.Message);
        }
    }

    /// <summary>
    /// Revoke all refresh tokens of a user with provided user id.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// <exception cref="TokenCacheException"></exception>
    public async Task RevokeAllRefreshTokensOfUser(string userId)
    {
        try
        {
            await EnsureRedisConnectionInitializedAsync();
            RedisValue[] setMembers = await _redisConnection!.ExecuteWithBackgroundReconnectAsync(async (db) => await db.SetMembersAsync(userId));
            foreach (var member in setMembers)
            {
                await RevokeRefreshToken(member.ToString());
            }
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, $"Failed to revoke all token of user {userId}. "
                + ex.Message);

            throw new TokenCacheException("Failed to revoke refresh token: " + ex.Message);
        }
        catch (Exception ex) when (ex is RedisConnectionException || ex is SocketException || ex is ObjectDisposedException)
        {
            _logger.LogError(ex, $"Failed to revoke all token of user {userId}. "
                + "An redis-related exception occurs while execute operation: " + ex.Message);

            throw new TokenCacheException("Failed to revoke refresh token: " + ex.Message);
        }
        catch (TokenCacheException ex)
        {
            _logger.LogError(ex, $"Failed to revoke all token of user {userId}.");

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to revoke all token of user {userId}."
                + $"Token validation failed due to unexpected exceptions: {ex.Message}");

            throw new TokenCacheException("Failed to revoke refresh token: " + ex.Message);
        }


    }

    /// <summary>
    /// Track user refresh token
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    /// <exception cref="TokenCacheException"></exception>
    public async Task TrackUserRefreshToken(Guid userId, string refreshToken)
    {
        try
        {
            await EnsureRedisConnectionInitializedAsync();
            await _redisConnection!.ExecuteWithBackgroundReconnectAsync(async (db) => await db.SetAddAsync(userId.ToString(), refreshToken));
        }
        catch (TimeoutException timeoutEx)
        {
            _logger.LogError($"An timeout exception occurs while ensure redis connection initialized: {timeoutEx.Message}");
            throw new TokenCacheException("Failed to track refresh token: " + timeoutEx.Message);

        }
        catch (Exception ex) when (ex is RedisConnectionException || ex is SocketException || ex is ObjectDisposedException)
        {
            _logger.LogError(ex, $"Tracking failed for token: {refreshToken}. Caching issue. "
                + "An redis-related exception occurs while execute operation: " + ex.Message);

            throw new TokenCacheException("Failed to track refresh token: " + ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Tracking failed for token: {refreshToken}. Caching issue. "
                + $"Token validation failed due to unexpected exceptions: {ex.Message}");

            throw new TokenCacheException("Failed to track refresh token: " + ex.Message);
        }
    }
}