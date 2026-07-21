using NotificationConnector.Core;
using NotificationConnector.Models;
using StackExchange.Redis;

namespace NotificationConnector.Adapters;

public class RedisSourceAdapter : ISourceAdapter
{
    private readonly ILogger<RedisSourceAdapter> _logger;
    private IConnectionMultiplexer? _connection;
    private ISubscriber? _subscriber;
    private string? _channelName;

    public RedisSourceAdapter(ILogger<RedisSourceAdapter> logger)
    {
        _logger = logger;
    }

    public string Name => "redis-adapter";

    public event Func<RawMessage, Task>? OnRawMessage;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var host = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
        _channelName = Environment.GetEnvironmentVariable("REDIS_CHANNEL") ?? "notifications";

        var connectionString = $"{host}:{port}";

        _connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
        _subscriber = _connection.GetSubscriber();

        var redisChannel = RedisChannel.Literal(_channelName);

        await _subscriber.SubscribeAsync(
            redisChannel,
            async (_, message) =>
            {
                var payload = message.ToString();

                if (string.IsNullOrWhiteSpace(payload))
                {
                    _logger.LogWarning("Redis source adapter received an empty message.");
                    return;
                }

                var rawMessage = new RawMessage(
                    AdapterName: Name,
                    Payload: payload,
                    ReceivedAt: DateTimeOffset.UtcNow
                );

                _logger.LogInformation("Redis source adapter received a raw message.");

                if (OnRawMessage is not null)
                {
                    await OnRawMessage.Invoke(rawMessage);
                }
            }
        );

        _logger.LogInformation(
            "Redis source adapter connected to {ConnectionString} and subscribed channel '{ChannelName}'.",
            connectionString,
            _channelName
        );
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Redis source adapter disconnected.");

        if (_subscriber is not null && _channelName is not null)
        {
            await _subscriber.UnsubscribeAsync(RedisChannel.Literal(_channelName));
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }
}