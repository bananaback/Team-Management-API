using AuthenticationService.Data;
using AuthenticationService.Models;
using AuthenticationService.Repositories.InboxRepositories;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.Tests.Repositories.InboxRepositories;

public class InboxRepositoryTest
{
    private readonly DbContextOptions<AuthenticationDbContext> _options;
    private readonly AuthenticationDbContext _context;

    public InboxRepositoryTest()
    {
        _options = new DbContextOptionsBuilder<AuthenticationDbContext>()
                        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                        .Options;
        _context = new AuthenticationDbContext(_options);
    }

    [Fact]
    public async Task CreateInboxMessage_Success_ReturnsInboxMessage()
    {
        // Arrange
        var inboxRepository = new InboxRepository(_context);
        InboxMessage inboxMessage = new InboxMessage()
        {
            MessageId = Guid.NewGuid(),
            EventData = "event_data",
            EventType = "User_Created",
            IsProcessedSuccessfully = false,
            TimeCreated = DateTime.Now
        };

        // Act
        var newlyCreateInboxMessage = await inboxRepository.CreateMessage(inboxMessage);
        await inboxRepository.UnitOfWork.SaveChangesAsync();
        var persistedInboxMessage = await _context.InboxMessages.FirstOrDefaultAsync(m => m.MessageId.ToString() == inboxMessage.MessageId.ToString());

        // Assert
        Assert.NotNull(newlyCreateInboxMessage);
        Assert.Equal(persistedInboxMessage, inboxMessage);

    }

    [Fact]
    public async Task CreateDuplicatedInBoxMessage_Fail_ThrowsException()
    {
        // Arrange
        var inboxRepository = new InboxRepository(_context);
        InboxMessage inboxMessage = new InboxMessage()
        {
            MessageId = Guid.NewGuid(),
            EventData = "event_data",
            EventType = "User_Created",
            IsProcessedSuccessfully = false,
            TimeCreated = DateTime.Now
        };

        // Act
        await inboxRepository.CreateMessage(inboxMessage);
        await inboxRepository.UnitOfWork.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await inboxRepository.CreateMessage(inboxMessage);
            await inboxRepository.UnitOfWork.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task DeleteMessage_Success_ReturnsNull()
    {
        // Arrange
        var inboxRepository = new InboxRepository(_context);
        InboxMessage inboxMessage = new InboxMessage()
        {
            MessageId = Guid.NewGuid(),
            EventData = "event_data",
            EventType = "User_Created",
            IsProcessedSuccessfully = false,
            TimeCreated = DateTime.Now
        };
        await _context.InboxMessages.AddAsync(inboxMessage);
        await _context.SaveChangesAsync();

        // Act
        await inboxRepository.DeleteMessage(inboxMessage);
        await inboxRepository.UnitOfWork.SaveChangesAsync();

        // Assert
        Assert.Null(await _context.InboxMessages.FirstOrDefaultAsync(m => m.MessageId.ToString() == inboxMessage.MessageId.ToString()));
    }

    [Fact]
    public async Task DeleteMessageThatNotExist_Fail_ThrowsException()
    {
        // Arrange
        var inboxRepository = new InboxRepository(_context);
        InboxMessage inboxMessage = new InboxMessage()
        {
            MessageId = Guid.NewGuid(),
            EventData = "event_data",
            EventType = "User_Created",
            IsProcessedSuccessfully = false,
            TimeCreated = DateTime.Now
        };

        // Act and Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            await inboxRepository.DeleteMessage(inboxMessage);
            await inboxRepository.UnitOfWork.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task UpdateMessage_Success_ReturnsNull()
    {
        // Arrange
        var inboxRepository = new InboxRepository(_context);
        InboxMessage inboxMessage = new InboxMessage()
        {
            MessageId = Guid.NewGuid(),
            EventData = "event_data",
            EventType = "User_Created",
            IsProcessedSuccessfully = false,
            TimeCreated = DateTime.Now
        };

        await _context.InboxMessages.AddAsync(inboxMessage);
        await _context.SaveChangesAsync();

        // Act
        var persistedInboxMessage = await _context.InboxMessages.FirstOrDefaultAsync(m => m.MessageId.ToString() == inboxMessage.MessageId.ToString());
        persistedInboxMessage!.EventData = "event_data2";
        await inboxRepository.UpdateMessage(persistedInboxMessage);
        await inboxRepository.UnitOfWork.SaveChangesAsync();

        // Assert
        var message = await _context.InboxMessages.FirstOrDefaultAsync(m => m.EventData == "event_data2");
        Assert.NotNull(message);
    }

    [Fact]
    public async Task UpdateMessageThatNotExist_Fail_ThrowsException()
    {
        // Arrange
        var inboxRepository = new InboxRepository(_context);
        InboxMessage inboxMessage = new InboxMessage()
        {
            MessageId = Guid.NewGuid(),
            EventData = "event_data",
            EventType = "User_Created",
            IsProcessedSuccessfully = false,
            TimeCreated = DateTime.Now
        };

        // Act and Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            await inboxRepository.UpdateMessage(inboxMessage);
            await inboxRepository.UnitOfWork.SaveChangesAsync();
        });

    }

    [Fact]
    public async Task GetUnProcessedMessages_Success_ReturnsListOfMessages()
    {
        // Arrange
        var inboxRepository = new InboxRepository(_context);

        // Act
        var unprocessedMessages = await inboxRepository.GetUnProcessedMessages();

        // Assert
        foreach (var message in unprocessedMessages)
        {
            Assert.False(message.IsProcessedSuccessfully);
        }
    }
}