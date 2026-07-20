using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NotificationConnector.Core;
using NotificationConnector.Models;

namespace NotificationConnector.Adapters;

public class RabbitMQSourceAdapter : ISourceAdapter
{
    private readonly ILogger<RabbitMQSourceAdapter> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMQSourceAdapter(ILogger<RabbitMQSourceAdapter> logger)
    {
        _logger = logger;
    }

    public string Name => "rabbitmq-adapter";

    public event Func<RawMessage, Task>? OnRawMessage;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var hostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        var portText = Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672";
        var userName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
        var password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
        var queueName = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "notifications";

        var port = int.Parse(portText);

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var payload = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            var rawMessage = new RawMessage(
                AdapterName: Name,
                Payload: payload,
                ReceivedAt: DateTimeOffset.UtcNow
            );

            _logger.LogInformation("RabbitMQ source adapter received a raw message.");

            if (OnRawMessage is not null)
            {
                await OnRawMessage.Invoke(rawMessage);
            }

            await _channel.BasicAckAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                cancellationToken: cancellationToken
            );
        };

        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken
        );

        _logger.LogInformation(
            "RabbitMQ source adapter connected to {HostName}:{Port} and listening queue '{QueueName}'.",
            hostName,
            port,
            queueName
        );
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ source adapter disconnected.");

        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
            await _connection.DisposeAsync();
        }
    }
}