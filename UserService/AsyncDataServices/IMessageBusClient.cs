using UserService.Dtos;
using UserService.Models;

namespace UserService.AsyncDataServices;

public interface IMessageBusClient : IDisposable
{
    bool Publish(PublishEventDto messageo);
}