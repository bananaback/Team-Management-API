namespace AuthenticationService.Services.CacheServices;

public interface ITokenCache
{
    public Task<bool> IsRefreshTokenRevoked(string refreshToken);
    public Task RevokeRefreshToken(string refreshToken);
    public Task RevokeAllRefreshTokensOfUser(string userId);
    public Task TrackUserRefreshToken(Guid userId, string refreshToken);

}