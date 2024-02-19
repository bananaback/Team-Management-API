namespace AuthenticationService.Models;

public class RefreshTokenClaims
{
    public string UserId { get; set; }
    public DateTime ExpirationDate { get; set; }

    public RefreshTokenClaims()
    {
        UserId = string.Empty;
    }

    public RefreshTokenClaims(string userId, DateTime expirationDate)
    {
        UserId = userId;
        ExpirationDate = expirationDate;
    }
}