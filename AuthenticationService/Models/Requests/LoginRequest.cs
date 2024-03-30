namespace AuthenticationService.Models.Requests;

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public LoginRequest()
    {
        Username = string.Empty;
        Password = string.Empty;
    }
    public LoginRequest(string username, string password)
    {
        Username = username;
        Password = password;
    }
}