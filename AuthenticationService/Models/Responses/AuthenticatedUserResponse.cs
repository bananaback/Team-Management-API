namespace AuthenticationService.Models.Responses;

public class AuthenticatedUserResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }

    public AuthenticatedUserResponse()
    {
        AccessToken = string.Empty;
        RefreshToken = string.Empty;
    }

    public AuthenticatedUserResponse(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }
}