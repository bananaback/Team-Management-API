using System.Text;
using AuthenticationService.Services.EventProcessingServices;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AuthenticationService.Services.AsyncDataServices;

public class MessageBusSubcriber : BackgroundService, IDisposable
{
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private string _queueName;
    private EventingBasicConsumer? _consumer;
    private string _consumerTag;
    private bool _isRetrying = false;

    public MessageBusSubcriber(IConfiguration configuration, IEventProcessor eventProcessor)
    {
        _configuration = configuration;
        _queueName = string.Empty;
        _consumerTag = string.Empty;
    }

    private Task TryConnectUntilSuccess()
    {
        return Task.Run(() =>
        {
            while (true)
            {
                try
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

                    _consumer = new EventingBasicConsumer(_channel);
                    _consumer.Received += HandleReceivedEvent;

                    _consumerTag = _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: _consumer);
                    _channel.CallbackException += HandleCallbackException;
                    _channel.ModelShutdown += HandleModelShutdown;

                    _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
                    _isRetrying = false;
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not connect to message bus: {ex.Message}");

                    DisposeChannelAndConnection();

                    Thread.Sleep(5000);
                }
            }
        });
    }

    private void HandleModelShutdown(object? sender, ShutdownEventArgs e)
    {
        // Handle model shutdown and initiate reconnection logic here
        Console.WriteLine($"ModelShutdown: {e.ReplyText}");
        if (!_isRetrying)
        {
            _isRetrying = true;
            DisposeChannelAndConnection();
            _ = TryConnectUntilSuccess();
        }
    }

    private void HandleCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        // Handle exception and initiate reconnection logic here
        Console.WriteLine($"CallbackException: {e.Exception.Message}");
        if (!_isRetrying)
        {
            _isRetrying = true;
            DisposeChannelAndConnection();
            _ = TryConnectUntilSuccess();
        }
    }

    private void RabbitMQ_ConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        Console.WriteLine("RabbitMQ connection shut down. Retrying...");
        if (!_isRetrying)
        {
            _isRetrying = true;
            DisposeChannelAndConnection();
            _ = TryConnectUntilSuccess();
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        if (!_isRetrying)
        {
            _isRetrying = true;
            DisposeChannelAndConnection();
            _ = TryConnectUntilSuccess();
        }

        return Task.CompletedTask;
    }

    private void HandleReceivedEvent(object? sender, BasicDeliverEventArgs ea)
    {
        Console.WriteLine("--> Event received!");
        try
        {
            var body = ea.Body;
            var notificationMessage = Encoding.UTF8.GetString(body.ToArray());

            // Processing the received event using the provided event processor
            //_eventProcessor.ProcessEventAsync(notificationMessage);
            Console.WriteLine("Event received:" + notificationMessage);

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
    }

    protected void Dispose(Boolean isItSafeToAlsoFreeManagedObjects)
    {
        //Free unmanaged resources
        DisposeChannelAndConnection();
        //Free managed resources too, but only if I'm being called from Dispose
        //(If I'm being called from Finalize then the objects might not exist
        //anymore
        if (isItSafeToAlsoFreeManagedObjects)
        {
            // Dispose managed objects
        }
    }

    private void DisposeChannelAndConnection()
    {
        try
        {
            if (_consumer != null)
            {
                _consumer.Received -= HandleReceivedEvent;

            }
            if (_channel != null)
            {
                _channel.BasicCancel(_consumerTag);
                _channel.CallbackException -= HandleCallbackException;
                _channel.ModelShutdown -= HandleModelShutdown;
                _channel.Close();
            }

            if (_connection != null)
            {
                _connection.ConnectionShutdown -= RabbitMQ_ConnectionShutdown;
                _connection.Close();
            }
            _consumer = null;
            _channel = null;
            _connection = null;
            Console.WriteLine("Consumer, channel and connection disposed");

        }
        catch (Exception ex)
        {
            // Handle or log any exceptions that occur during disposal
            Console.WriteLine($"Error during dispose channel and connection: {ex.Message}");
        }
    }

    public override void Dispose()
    {
        try
        {
            Dispose(true);
        }
        finally
        {
            base.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    ~MessageBusSubcriber()
    {
        Dispose(false);
    }
}