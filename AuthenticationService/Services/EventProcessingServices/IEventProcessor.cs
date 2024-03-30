namespace AuthenticationService.Services.EventProcessingServices;

public interface IEventProcessor
{
    Task ProcessEventAsync(string message);
}