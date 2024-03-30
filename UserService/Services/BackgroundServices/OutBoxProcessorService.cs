
using AutoMapper;
using Services.OutboxMessageServices;
using UserService.AsyncDataServices;
using UserService.Dtos;
using UserService.Models;
using UserService.Repositories;

namespace Services.BackgroundServices;

public class OutboxProcessorService : BackgroundService
{
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly IMessageBusClient _messageBusClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMapper _mapper;

    public OutboxProcessorService(ILogger<OutboxProcessorService> logger, IMessageBusClient messageBusClient, IServiceScopeFactory serviceScopeFactory, IMapper mapper)
    {
        _logger = logger;
        _messageBusClient = messageBusClient;
        _serviceScopeFactory = serviceScopeFactory;
        _mapper = mapper;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(5000, stoppingToken);

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var outboxMessageService = scope.ServiceProvider.GetRequiredService<IOutboxMessageService>();
                IEnumerable<OutboxMessage> unsentMessages = await outboxMessageService.GetUnsentOutboxMessages();
                List<OutboxMessage> orderedMessages = unsentMessages.OrderBy(m => m.TimeCreated).ToList();
                for (int i = 0; i < orderedMessages.Count; i++)
                {
                    var message = orderedMessages[i];

                    // Send async message
                    try
                    {
                        PublishEventDto publishEvent = _mapper.Map<PublishEventDto>(message);
                        if (_messageBusClient.Publish(publishEvent))
                        {
                            message.IsSent = true;
                            await outboxMessageService.UpdateOutboxMessage(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"--> Could not send asynchronously: {ex.Message}");
                    }
                }
            }

            _logger.LogInformation("Hello from background :)");
        }
    }
}