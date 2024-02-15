using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;

namespace UserService.Repositories.OutboxRepositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly UserDbContext _dbContext;
    public OutboxRepository(UserDbContext userDbContext)
    {
        _dbContext = userDbContext;
    }
    public IUnitOfWork UnitOfWork => _dbContext;

    public async Task<OutboxMessage> Create(OutboxMessage outboxMessage)
    {
        await _dbContext.AddAsync(outboxMessage);
        return outboxMessage;
    }

    public async Task<IEnumerable<OutboxMessage>> GetUnsentMessages()
    {
        IEnumerable<OutboxMessage> unsentMessages = await _dbContext.OutboxMessages.Where(o => o.IsSent == false).ToListAsync();
        return unsentMessages;
    }

    public async Task UpdateMessage(OutboxMessage message)
    {
        _dbContext.Update(message);
        await Task.CompletedTask;
    }
}