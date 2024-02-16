namespace AuthenticationService.Services.EventProcessingServices;

public interface IEventProcessor
{
    void ProcessEvent(string message);
}