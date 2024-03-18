using AuthenticationService.Exceptions;
using AuthenticationService.Models;
using AuthenticationService.Models.Requests;
using AuthenticationService.Models.Response;
using AuthenticationService.Repositories.UserRepositories;
using AuthenticationService.Services.CacheServices;
using AuthenticationService.Services.PasswordHashers;
using AuthenticationService.Services.TokenGenerators;
using AuthenticationService.Services.TokenValidators;
using Microsoft.AspNetCore.Identity;

namespace AuthenticationService.Services.Authenticators;

public class Authenticator
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AccessTokenGenerator _accessTokenGenerator;
    private readonly RefreshTokenGenerator _refreshTokenGenerator;
    private readonly RefreshTokenValidator _refreshTokenValidator;
    private readonly RedisTokenCache _redisTokenCache;
    private readonly ILogger<Authenticator> _logger;

    public Authenticator(IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        AccessTokenGenerator accessTokenGenerator,
        RefreshTokenGenerator refreshTokenGenerator,
        RedisTokenCache redisTokenCache,
        RefreshTokenValidator refreshTokenValidator,
        ILogger<Authenticator> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _redisTokenCache = redisTokenCache;
        _refreshTokenValidator = refreshTokenValidator;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user with username and password
    /// </summary>
    /// <param name="loginRequest"></param>
    /// <returns cref="AuthenticatedUserResponse">Returns AuthenticatedUserResponse includes access token and refresh token.</returns>
    /// <exception cref="AuthenticationFailedException"></exception>
    public async Task<AuthenticatedUserResponse> Authenticate(LoginRequest loginRequest)
    {

        ApplicationUser? user = await _userRepository.GetByUsername(loginRequest.Username);
        if (user is not null)
        {
            if (_passwordHasher.VerifyPassword(loginRequest.Password, user.PasswordHash))
            {
                AuthenticatedUserResponse authenticatedUserResponse = new AuthenticatedUserResponse()
                {
                    AccessToken = _accessTokenGenerator.GenerateToken(user),
                    RefreshToken = _refreshTokenGenerator.GenerateToken(user)
                };
                try
                {
                    _redisTokenCache.TrackUserRefreshToken(user.UserId, authenticatedUserResponse.RefreshToken);
                }
                catch (TokenCacheException ex)
                {
                    _logger.LogError(ex, "Authentication failed: could not perform token cache");

                    throw new AuthenticationFailedException("Authentication failed: could not perform token cache.");
                }
                _logger.LogInformation($"User with username: {loginRequest.Username} logged in success at: {DateTime.Now}");
                return authenticatedUserResponse;
            }
            else
            {
                _logger.LogInformation($"User with username: {loginRequest.Username} logged in failed at: {DateTime.Now} with password: {loginRequest.Password}");

                throw new AuthenticationFailedException("Authentication failed: Wrong password.", isUserFault: true);
            }
        }
        else
        {
            throw new AuthenticationFailedException("Authentication failed: User not found.", isUserFault: true);
        }

    }

    /// <summary>
    /// Log user out by revoke refresh token
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    /// <exception cref="AuthenticationFailedException"></exception>
    public async Task LogUserOut(string refreshToken)
    {
        try
        {
            await _redisTokenCache.RevokeRefreshToken(refreshToken);
            _logger.LogInformation($"User logged out success at: {DateTime.Now}. Provided token: {refreshToken}");

        }
        catch (TokenCacheException ex)
        {
            _logger.LogError(ex, "Failed to log user out due to token caching issue.");

            throw new AuthenticationFailedException("Authentication failed: Could not log user out due to caching issue.");
        }
    }

    /// <summary>
    /// Log user out on all devices by revoking all living refesh tokens of that user
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// <exception cref="AuthenticationFailedException"></exception>
    public async Task LogUserOutOnAllDevices(string userId)
    {
        try
        {
            await _redisTokenCache.RevokeAllRefreshTokensOfUser(userId);
            _logger.LogInformation($"User with id: {userId} logged out success on all devices at: {DateTime.Now}.");

        }
        catch (TokenCacheException ex)
        {
            _logger.LogError(ex, "Failed to log all users out due to token caching issue.");

            throw new AuthenticationFailedException("Authentication failed: Could not log all users out due to caching issue.");
        }
    }

    /// <summary>
    /// Rotate refresh token
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    /// <exception cref="AuthenticationFailedException"></exception>
    public async Task<AuthenticatedUserResponse> RotateToken(string refreshToken)
    {
        bool refreshTokenRevoked = await _redisTokenCache.IsRefreshTokenRevoked(refreshToken);
        if (refreshTokenRevoked)
        {
            _logger.LogError($"Authentication failed: could not rotate the token because it is revoked. Provided token {refreshToken}");

            throw new AuthenticationFailedException("Authentication failed: could not rotate the token because it is revoked.", isUserFault: true);
        }
        try
        {
            RefreshTokenClaims refreshTokenClaims = _refreshTokenValidator.ExtractTokenClaims(refreshToken);

            ApplicationUser? user = await _userRepository.GetById(Guid.Parse(refreshTokenClaims.UserId));
            if (user is not null)
            {
                AuthenticatedUserResponse authenticatedUserResponse = new AuthenticatedUserResponse()
                {
                    AccessToken = _accessTokenGenerator.GenerateToken(user),
                    RefreshToken = _refreshTokenGenerator.GenerateToken(user)
                };
                _redisTokenCache.TrackUserRefreshToken(user.UserId, authenticatedUserResponse.RefreshToken);
                return authenticatedUserResponse;
            }
            else
            {
                _logger.LogError($"Authentication failed: could not rotate the token because user with id: {refreshTokenClaims.UserId} not found.");

                throw new AuthenticationFailedException(
                    $"Authentication failed: could not rotate the token because user with id: {refreshTokenClaims.UserId} not found.", isUserFault: true);

            }

        }
        catch (RequiredTokenClaimNotFoundException ex)
        {
            _logger.LogError(ex, $"Authentication failed: could not rotate the token because required claims not found. Provided token: {refreshToken}");

            throw new AuthenticationFailedException(
                $"Authentication failed: could not rotate the token because required claims not found. Provided token: {refreshToken} {ex.Message}", isUserFault: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Authentication failed due to an unexpected exception. Provided token: {refreshToken}");

            throw new AuthenticationFailedException($"Authentication failed due to an unexpected exception. Provided token: {refreshToken}");
        }
    }
}