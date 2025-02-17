using System.Net.Sockets;
using StackExchange.Redis;

namespace AuthenticationService.Services.CacheService;

public class RedisConnection : IDisposable
{
    private long _lastReconnectTicks = DateTimeOffset.MinValue.UtcTicks;
    private DateTimeOffset _firstErrorTime = DateTimeOffset.MinValue;
    private DateTimeOffset _previousErrorTime = DateTimeOffset.MinValue;

    // StackExchange.Redis will also be trying to reconnect internally,
    // so limit how often we recreate the ConnectionMultiplexer instance
    // in an attempt to reconnect
    private readonly TimeSpan ReconnectMinInterval = TimeSpan.FromSeconds(60);

    // If errors occur for longer than this threshold, StackExchange.Redis
    // may be failing to reconnect internally, so we'll recreate the
    // ConnectionMultiplexer instance
    private readonly TimeSpan ReconnectErrorThreshold = TimeSpan.FromSeconds(30);
    private readonly TimeSpan RestartConnectionTimeout = TimeSpan.FromSeconds(15);
    private const int RetryMaxAttempts = 5;

    private SemaphoreSlim _reconnectSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);
    private readonly string _connectionString;
    private ConnectionMultiplexer? _connection;
    private IDatabase? _database;

    private RedisConnection(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static async Task<RedisConnection> InitializeAsync(string connectionString)
    {
        var redisConnection = new RedisConnection(connectionString);
        try
        {
            await redisConnection.ForceReconnectAsync(initializing: true);
        }
        catch (ObjectDisposedException) { }

        return redisConnection;
    }

    // In real applications, consider using a framework such as
    // Polly to make it easier to customize the retry approach.
    // For more info, please see: https://github.com/App-vNext/Polly
    /// <summary>
    /// Execute the operation with redis, if operation failed due to connection issue, try to reconnect in the background and throw the exception instantly for client
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="func"></param>
    /// <returns></returns>
    /// <exception cref="RedisConnectionException">Indicates a connection fault when communicating with redis.</exception>
    /// <exception cref="SocketException">The exception that is thrown when a socket error occurs.</exception>
    /// <exception cref="ObjectDisposedException">The exception that is thrown when an operation is performed on a disposed object.</exception>

    public async Task<T> ExecuteWithBackgroundReconnectAsync<T>(Func<IDatabase, Task<T>> func)
    {
        try
        {
            return await func(_database!);
        }
        catch (Exception ex) when (ex is RedisConnectionException || ex is SocketException || ex is ObjectDisposedException)
        {
            // Try to reconnect in the background before rethrowing the exception instantly
            _ = Task.Run(async () =>
            {
                try
                {
                    await ForceReconnectAsync();
                }
                catch (ObjectDisposedException) { }
            });

            // Rethrow the exception instantly
            throw;
        }
    }


    /// <summary>
    /// Force a new ConnectionMultiplexer to be created.
    /// NOTES:
    ///     1. Users of the ConnectionMultiplexer MUST handle ObjectDisposedExceptions, which can now happen as a result of calling ForceReconnectAsync().
    ///     2. Call ForceReconnectAsync() for RedisConnectionExceptions and RedisSocketExceptions. You can also call it for RedisTimeoutExceptions,
    ///         but only if you're using generous ReconnectMinInterval and ReconnectErrorThreshold. Otherwise, establishing new connections can cause
    ///         a cascade failure on a server that's timing out because it's already overloaded.
    ///     3. The code will:
    ///         a. wait to reconnect for at least the "ReconnectErrorThreshold" time of repeated errors before actually reconnecting
    ///         b. not reconnect more frequently than configured in "ReconnectMinInterval"
    /// </summary>
    /// <param name="initializing">Should only be true when ForceReconnect is running at startup.</param>
    private async Task ForceReconnectAsync(bool initializing = false)
    {
        long previousTicks = Interlocked.Read(ref _lastReconnectTicks);
        var previousReconnectTime = new DateTimeOffset(previousTicks, TimeSpan.Zero);
        TimeSpan elapsedSinceLastReconnect = DateTimeOffset.UtcNow - previousReconnectTime;

        // We want to limit how often we perform this top-level reconnect, so we check how long it's been since our last attempt.
        if (elapsedSinceLastReconnect < ReconnectMinInterval)
        {
            return;
        }

        bool lockTaken = await _reconnectSemaphore.WaitAsync(RestartConnectionTimeout);
        if (!lockTaken)
        {
            // If we fail to enter the semaphore, then it is possible that another thread has already done so.
            // ForceReconnectAsync() can be retried while connectivity problems persist.
            return;
        }

        try
        {
            var utcNow = DateTimeOffset.UtcNow;
            previousTicks = Interlocked.Read(ref _lastReconnectTicks);
            previousReconnectTime = new DateTimeOffset(previousTicks, TimeSpan.Zero);
            elapsedSinceLastReconnect = utcNow - previousReconnectTime;

            if (_firstErrorTime == DateTimeOffset.MinValue && !initializing)
            {
                // We haven't seen an error since last reconnect, so set initial values.
                _firstErrorTime = utcNow;
                _previousErrorTime = utcNow;
                return;
            }

            if (elapsedSinceLastReconnect < ReconnectMinInterval)
            {
                return; // Some other thread made it through the check and the lock, so nothing to do.
            }

            TimeSpan elapsedSinceFirstError = utcNow - _firstErrorTime;
            TimeSpan elapsedSinceMostRecentError = utcNow - _previousErrorTime;

            bool shouldReconnect =
                elapsedSinceFirstError >= ReconnectErrorThreshold // Make sure we gave the multiplexer enough time to reconnect on its own if it could.
                && elapsedSinceMostRecentError <= ReconnectErrorThreshold; // Make sure we aren't working on stale data (e.g. if there was a gap in errors, don't reconnect yet).

            // Update the previousErrorTime timestamp to be now (e.g. this reconnect request).
            _previousErrorTime = utcNow;

            if (!shouldReconnect && !initializing)
            {
                return;
            }

            _firstErrorTime = DateTimeOffset.MinValue;
            _previousErrorTime = DateTimeOffset.MinValue;

            // Create a new connection
            ConnectionMultiplexer _newConnection = await ConnectionMultiplexer.ConnectAsync(_connectionString);

            // Swap current connection with the new connection
            ConnectionMultiplexer? oldConnection = Interlocked.Exchange(ref _connection, _newConnection);

            Interlocked.Exchange(ref _lastReconnectTicks, utcNow.UtcTicks);
            IDatabase newDatabase = _connection.GetDatabase();
            Interlocked.Exchange(ref _database, newDatabase);

            if (oldConnection != null)
            {
                try
                {
                    await oldConnection.CloseAsync();
                }
                catch
                {
                    // Ignore any errors from the old connection
                }
            }
        }
        finally
        {
            _reconnectSemaphore.Release();
        }
    }

    protected void Dispose(bool isItSafeToDisposeManageObjects)
    {
        _connection?.Dispose();
        if (isItSafeToDisposeManageObjects)
        {

        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~RedisConnection()
    {
        Dispose(false);
    }
}