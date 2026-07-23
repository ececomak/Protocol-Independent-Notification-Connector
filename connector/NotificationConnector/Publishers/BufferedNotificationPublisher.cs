using System.Threading.Channels;
using NotificationConnector.Models;

namespace NotificationConnector.Publishers;

public class BufferedNotificationPublisher : BackgroundService, INotificationPublisher
{
    private readonly Channel<NotificationEnvelope> _channel;
    private readonly Queue<NotificationEnvelope> _pendingNotifications = new();
    private readonly BackendNotificationPublisher _backendPublisher;
    private readonly ILogger<BufferedNotificationPublisher> _logger;

    public BufferedNotificationPublisher(
        BackendNotificationPublisher backendPublisher,
        ILogger<BufferedNotificationPublisher> logger)
    {
        _backendPublisher = backendPublisher;
        _logger = logger;

        _channel = Channel.CreateUnbounded<NotificationEnvelope>();
    }

    public async Task PublishAsync(
        NotificationEnvelope notification,
        CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(notification, cancellationToken);

        _logger.LogInformation(
            "Notification added to retry buffer. Source: {Source}, Type: {Type}, Title: {Title}",
            notification.Source,
            notification.Type,
            notification.Title
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Buffered notification publisher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MoveNewMessagesToPendingQueueAsync(stoppingToken);
                await TrySendPendingMessagesAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in buffered notification publisher.");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }

        _logger.LogInformation("Buffered notification publisher stopped.");
    }

    private async Task MoveNewMessagesToPendingQueueAsync(CancellationToken cancellationToken)
    {
        while (_channel.Reader.TryRead(out var notification))
        {
            _pendingNotifications.Enqueue(notification);
        }

        if (_pendingNotifications.Count == 0)
        {
            await _channel.Reader.WaitToReadAsync(cancellationToken);
        }

        while (_channel.Reader.TryRead(out var notification))
        {
            _pendingNotifications.Enqueue(notification);
        }
    }

    private async Task TrySendPendingMessagesAsync(CancellationToken cancellationToken)
    {
        while (_pendingNotifications.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var notification = _pendingNotifications.Peek();

            var published = await _backendPublisher.TryPublishAsync(
                notification,
                cancellationToken
            );

            if (!published)
            {
                _logger.LogWarning(
                    "Notification could not be published. Pending buffer count: {PendingCount}",
                    _pendingNotifications.Count
                );

                return;
            }

            _pendingNotifications.Dequeue();

            _logger.LogInformation(
                "Notification removed from retry buffer. Pending buffer count: {PendingCount}",
                _pendingNotifications.Count
            );
        }
    }
}