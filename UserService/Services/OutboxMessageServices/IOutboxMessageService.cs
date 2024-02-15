using UserService.Models;

namespace Services.OutboxMessageServices;

public interface IOutboxMessageService
{
    Task<IEnumerable<OutboxMessage>> GetUnsentOutboxMessages();
    Task UpdateOutboxMessage(OutboxMessage outboxMessage);
}