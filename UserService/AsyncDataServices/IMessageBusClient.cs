using UserService.Dtos;
using UserService.Models;

namespace UserService.AsyncDataServices;

public interface IMessageBusClient
{
    void Publish(OutboxMessage messageo);
}