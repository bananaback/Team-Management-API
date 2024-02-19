namespace AuthenticationService.Exceptions;

public class UserAlreadyExistException : Exception
{
    public UserAlreadyExistException() : base()
    {

    }
    public UserAlreadyExistException(string message) : base(message)
    {

    }
}