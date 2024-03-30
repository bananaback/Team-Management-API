using AuthenticationService.Data;
using AuthenticationService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.Repositories.InboxRepositories;

public class InboxRepository : IInboxRepository
{
    private readonly AuthenticationDbContext _dbContext;
    public InboxRepository(AuthenticationDbContext authenticationDbContext)
    {
        _dbContext = authenticationDbContext;
    }
    public IUnitOfWork UnitOfWork => _dbContext;

    public async Task<InboxMessage> CreateMessage(InboxMessage message)
    {
        await _dbContext.InboxMessages.AddAsync(message);
        return message;
    }

    public async Task DeleteMessage(InboxMessage message)
    {
        _dbContext.InboxMessages.Remove(message);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<InboxMessage>> GetUnProcessedMessages()
    {
        IEnumerable<InboxMessage> unsuccessfulProcessedMessages =
        await _dbContext.InboxMessages.Where(i => i.IsProcessedSuccessfully == false).ToListAsync();
        return unsuccessfulProcessedMessages;
    }

    public Task UpdateMessage(InboxMessage message)
    {
        _dbContext.Update(message);
        return Task.CompletedTask;
    }
}