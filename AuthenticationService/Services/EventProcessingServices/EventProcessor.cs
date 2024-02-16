using System.Text.Json;
using AuthenticationService.Dtos;
using AutoMapper;

namespace AuthenticationService.Services.EventProcessingServices;

public class EventProcessor : IEventProcessor
{
    private IServiceScopeFactory _serviceScopeFactory;

    public EventProcessor(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }
    public void ProcessEvent(string message)
    {
        EventType eventType = DetermineEvent(message);
        switch (eventType)
        {
            case EventType.User_Created:
                // TO DO
                break;
            default:
                break;
        }
    }

    private EventType DetermineEvent(string message)
    {
        Console.WriteLine("--> Determining event");
        try
        {
            PublishEventDto? publishEventDto = JsonSerializer.Deserialize<PublishEventDto>(message);
            switch (publishEventDto?.EventType)
            {
                case "User_Created":
                    Console.WriteLine("--> User created event detected");
                    Console.WriteLine(publishEventDto.EventData);
                    return EventType.User_Created;
                default:
                    Console.WriteLine("--> Could not determine event type");
                    return EventType.Undetermined;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not deserialize message: {ex.Message}");
            return EventType.Undetermined;
        }

    }
}

enum EventType
{
    User_Created,
    User_Deleted,
    Undetermined
}