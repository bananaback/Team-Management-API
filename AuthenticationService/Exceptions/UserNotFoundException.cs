namespace AuthenticationService.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException()
    {

    }

    public UserNotFoundException(String message) : base(message)
    {

    }
}