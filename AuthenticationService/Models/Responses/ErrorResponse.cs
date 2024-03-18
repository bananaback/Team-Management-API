namespace AuthenticationService.Models.Responses;

public class ErrorResponse
{
    public IEnumerable<string> ErrorMessages { get; set; }
    public ErrorResponse(string errorMessage) : this(new List<string>() { errorMessage }) { }
    public ErrorResponse(IEnumerable<string> errorMessages)
    {
        this.ErrorMessages = errorMessages;
    }
}