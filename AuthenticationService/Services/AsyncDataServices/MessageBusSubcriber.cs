using System.Text;
using AuthenticationService.Services.EventProcessingServices;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AuthenticationService.Services.AsyncDataServices;

public class MessageBusSubcriber : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IEventProcessor _eventProcessor;
    private IConnection? _connection;
    private IModel? _channel;
    private string _queueName;

    public MessageBusSubcriber(IConfiguration configuration, IEventProcessor eventProcessor)
    {
        _configuration = configuration;
        _eventProcessor = eventProcessor;
        _queueName = string.Empty;
        InitializeRabbitMQ();
    }

    private void InitializeRabbitMQ()
    {
        Console.WriteLine(_configuration["RabbitMQHost"]);
        Console.WriteLine(_configuration["RabbitMQPort"]);

        var factory = new ConnectionFactory()
        {
            HostName = _configuration["RabbitMQHost"],
            Port = int.Parse(_configuration["RabbitMQPort"]!)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange: "trigger", type: ExchangeType.Fanout, durable: true);
        _queueName = _channel.QueueDeclare(queue: "auth_service_subcriber_queue", durable: true, exclusive: false, autoDelete: false, arguments: null).QueueName;
        _channel.QueueBind(queue: _queueName, exchange: "trigger", routingKey: "");
        Console.WriteLine("--> Listening on the message bus...");

        _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
    }

    private void RabbitMQ_ConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        Console.WriteLine("RabbitMQ connection shut down");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (ModuleHandle, ea) =>
        {
            Console.WriteLine("--> Event received!");
            try
            {
                var body = ea.Body;
                var notificationMessage = Encoding.UTF8.GetString(body.ToArray());

                // Processing the received event using the provided event processor
                _eventProcessor.ProcessEventAsync(notificationMessage);

                // Manually acknowledging the message
                _channel?.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                // Handle exception, log, or perform any necessary error handling
                Console.WriteLine($"Error processing message: {ex.Message}");

                // Manually rejecting the message without requeuing
                _channel?.BasicReject(ea.DeliveryTag, requeue: false);
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        if (_channel!.IsOpen)
        {
            _channel.Close();
        }
        base.Dispose();
    }
}