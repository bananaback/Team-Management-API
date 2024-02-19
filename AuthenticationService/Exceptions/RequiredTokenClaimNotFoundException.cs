namespace AuthenticationService.Exceptions;

public class RequiredTokenClaimNotFoundException : Exception
{
    public RequiredTokenClaimNotFoundException() : base()
    {

    }

    public RequiredTokenClaimNotFoundException(string message) : base(message)
    {

    }
}