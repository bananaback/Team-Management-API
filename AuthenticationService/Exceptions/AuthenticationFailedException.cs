namespace AuthenticationService.Exceptions;

public class AuthenticationFailedException : Exception
{
    public bool IsUserFault { get; set; } = false;
    public AuthenticationFailedException() : base() { }
    public AuthenticationFailedException(string message) : base(message) { }
    public AuthenticationFailedException(string message, bool isUserFault)
    {
        IsUserFault = isUserFault;
    }
}