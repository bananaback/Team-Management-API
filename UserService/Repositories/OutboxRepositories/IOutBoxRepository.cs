using UserService.Models;

namespace UserService.Repositories.OutboxRepositories;

public interface IOutboxRepository
{
    IUnitOfWork UnitOfWork { get; }
    Task<IEnumerable<OutboxMessage>> GetUnsentMessages();
    Task<OutboxMessage> Create(OutboxMessage outboxMessage);
    Task UpdateMessage(OutboxMessage message);
}