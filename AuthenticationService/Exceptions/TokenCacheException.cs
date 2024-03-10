namespace AuthenticationService.Exceptions;

public class TokenCacheException : Exception
{
    public TokenCacheException() : base()
    {

    }

    public TokenCacheException(string message) : base(message)
    {

    }
}