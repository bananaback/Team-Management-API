using System.Text.Json;
using AuthenticationService.Dtos;
using AuthenticationService.Services.UserServices;
using AutoMapper;

namespace AuthenticationService.Services.EventProcessingServices;

public class EventProcessor : IEventProcessor
{
    private IServiceScopeFactory _serviceScopeFactory;

    public EventProcessor(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }
    public async Task ProcessEventAsync(string message)
    {
        EventType eventType = DetermineEvent(message);
        switch (eventType)
        {
            case EventType.User_Created:
                PublishEventDto? publishEventDto = JsonSerializer.Deserialize<PublishEventDto>(message);
                ApplicationUserCreateDto? user = JsonSerializer.Deserialize<ApplicationUserCreateDto>(publishEventDto!.EventData);
                if (user is not null)
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                        await userService.CreateUser(user);
                    }
                }
                else
                {
                    Console.WriteLine("Could not deserialize user.");
                }
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