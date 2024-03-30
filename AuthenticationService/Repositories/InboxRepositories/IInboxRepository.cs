using AuthenticationService.Models;

namespace AuthenticationService.Repositories;

public interface IInboxRepository
{
    IUnitOfWork UnitOfWork { get; }
    Task<IEnumerable<InboxMessage>> GetUnProcessedMessages();
    Task<InboxMessage> CreateMessage(InboxMessage message);
    Task UpdateMessage(InboxMessage message);
    Task DeleteMessage(InboxMessage message);
}