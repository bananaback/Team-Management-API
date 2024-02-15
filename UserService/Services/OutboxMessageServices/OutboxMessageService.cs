using Services.OutboxMessageServices;
using UserService.Models;
using UserService.Repositories.OutboxRepositories;

namespace UserService.OutboxMessageServices;

public class OutboxMessageService : IOutboxMessageService
{
    private readonly IOutboxRepository _outboxRepository;
    public OutboxMessageService(IOutboxRepository outboxRepository)
    {
        _outboxRepository = outboxRepository;
    }
    public Task<IEnumerable<OutboxMessage>> GetUnsentOutboxMessages()
    {
        return _outboxRepository.GetUnsentMessages();
    }

    public async Task UpdateOutboxMessage(OutboxMessage outboxMessage)
    {
        await _outboxRepository.UpdateMessage(outboxMessage);
        await _outboxRepository.UnitOfWork.SaveChangesAsync();
    }
}