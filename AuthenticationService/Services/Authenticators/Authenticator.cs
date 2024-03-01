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

    public Authenticator(IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        AccessTokenGenerator accessTokenGenerator,
        RefreshTokenGenerator refreshTokenGenerator,
        RedisTokenCache redisTokenCache,
        RefreshTokenValidator refreshTokenValidator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _redisTokenCache = redisTokenCache;
        _refreshTokenValidator = refreshTokenValidator;
    }
    public async Task<AuthenticatedUserResponse?> Authenticate(LoginRequest loginRequest)
    {
        AuthenticatedUserResponse? authenticatedUserResponse = null;

        ApplicationUser? user = await _userRepository.GetByUsername(loginRequest.Username);
        if (user is not null)
        {
            if (_passwordHasher.VerifyPassword(loginRequest.Password, user.PasswordHash))
            {
                authenticatedUserResponse = new AuthenticatedUserResponse()
                {
                    AccessToken = _accessTokenGenerator.GenerateToken(user),
                    RefreshToken = _refreshTokenGenerator.GenerateToken(user)
                };
                _redisTokenCache.TrackUserRefreshToken(user.UserId, authenticatedUserResponse.RefreshToken);
            }
        }

        return authenticatedUserResponse;
    }

    public async Task LogUserOut(string refreshToken)
    {
        await _redisTokenCache.RevokeRefreshToken(refreshToken);
    }

    public async Task LogUserOutOnAllDevices(string userId)
    {
        await _redisTokenCache.RevokeAllRefreshTokensOfUser(userId);
    }

    public async Task<AuthenticatedUserResponse?> RotateToken(string refreshToken)
    {
        AuthenticatedUserResponse? authenticatedUserResponse = null;
        bool refreshTokenRevoked = await _redisTokenCache.IsRefreshTokenRevoked(refreshToken);
        if (refreshTokenRevoked)
        {
            return authenticatedUserResponse;
        }
        try
        {
            RefreshTokenClaims refreshTokenClaims = _refreshTokenValidator.ExtractTokenClaims(refreshToken);

            ApplicationUser? user = await _userRepository.GetById(Guid.Parse(refreshTokenClaims.UserId));
            if (user is not null)
            {
                authenticatedUserResponse = new AuthenticatedUserResponse()
                {
                    AccessToken = _accessTokenGenerator.GenerateToken(user),
                    RefreshToken = _refreshTokenGenerator.GenerateToken(user)
                };
                _redisTokenCache.TrackUserRefreshToken(user.UserId, authenticatedUserResponse.RefreshToken);
            }

        }
        catch (RequiredTokenClaimNotFoundException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token validation failed: {ex.Message}");
        }
        return authenticatedUserResponse;
    }
}