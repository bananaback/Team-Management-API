namespace AuthenticationService.Exceptions;

public class TokenExtractionException : Exception
{
    public TokenExtractionException() : base()
    {

    }

    public TokenExtractionException(string message) : base(message)
    {

    }
}