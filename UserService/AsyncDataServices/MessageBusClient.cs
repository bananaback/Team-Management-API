using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using UserService.Dtos;
using UserService.Models;
using System.Threading;

namespace UserService.AsyncDataServices;

public class MessageBusClient : IMessageBusClient, IDisposable
{
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _isAvailable = false;
    private bool _isConnecting = false;

    public MessageBusClient(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private Task Connect()
    {
        return Task.Run(() =>
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQHost"],
                Port = int.Parse(_configuration["RabbitMQPort"]!)
            };
            while (true)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();

                    _channel.ExchangeDeclare(exchange: "trigger", type: ExchangeType.Fanout, durable: true);

                    _channel.QueueDeclare(
                    queue: "auth_service_subcriber_queue",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                    _channel.QueueBind(
                    queue: "auth_service_subcriber_queue",
                    exchange: "trigger",
                    routingKey: "");

                    _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
                    _isAvailable = true;
                    _isConnecting = false;
                    Console.WriteLine("--> Connected to message bus.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"--> Could not connect to the message bus: {ex.Message}");
                    Console.WriteLine("Connection and channel may be null!");

                    DisposeChannelAndConnection();

                    Thread.Sleep(5000);
                }
            }
        });
    }

    private void RabbitMQ_ConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        Console.WriteLine("--> RabbitMQ connection shutdown. Retrying...");
        _isAvailable = false;
        _isConnecting = false;
    }

    public bool Publish(PublishEventDto message)
    {
        if (!_isAvailable)
        {
            if (!_isConnecting)
            {
                _isConnecting = true;
                _ = Connect();
                Console.WriteLine("The connection is not available but we are trying to reconnect...");
            }
            return false;
        }
        var serializedMessage = JsonSerializer.Serialize(message);

        if (_connection != null && _connection.IsOpen)
        {
            Console.WriteLine("--> RabbitMQ connection open, sending message...");
            if (SendMessage(serializedMessage))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            Console.WriteLine("--> RabbitMQ connection is null or closed, not sending");
            return false;
        }
    }

    private bool SendMessage(string message)
    {
        if (!_isAvailable)
        {
            if (!_isConnecting)
            {
                _isConnecting = true;
                _ = Connect();
                Console.WriteLine("The connection is not available but we are trying to reconnect...");
            }
            return false;
        }
        if (_channel == null || _channel.IsClosed)
        {
            Console.WriteLine("--> Could not send message, channel is null or close.");
            return false;
        }
        var body = Encoding.UTF8.GetBytes(message);
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        _channel.BasicPublish(
            exchange: "trigger",
            routingKey: "",
            basicProperties: properties,
            body: body);
        Console.WriteLine($"--> We have sent {message}");
        return true;
    }

    public void DisposeChannelAndConnection()
    {
        try
        {
            if (_channel != null)
            {
                _channel.Close();
            }

            if (_connection != null)
            {
                _connection.ConnectionShutdown -= RabbitMQ_ConnectionShutdown;
                _connection.Close();
            }
            _channel = null;
            _connection = null;
            Console.WriteLine("Channel and connection disposed");

        }
        catch (Exception ex)
        {
            // Handle or log any exceptions that occur during disposal
            Console.WriteLine($"Error during dispose channel and connection: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Dispose(true); //I am calling you from Dispose, it's safe
        GC.SuppressFinalize(this); //Hey, GC: don't bother calling finalize later
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

    ~MessageBusClient()
    {
        Dispose(false); //I am *not* calling you from Dispose, it's *not* safe
    }
}

