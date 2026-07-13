using NotificationConnector.Adapters;
using NotificationConnector.Core;
using NotificationConnector.Publishers;

namespace NotificationConnector;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConnector _connector;
    private readonly MockSourceAdapter _mockSourceAdapter;
    private readonly WebhookSourceAdapter _webhookSourceAdapter;
    private readonly INotificationPublisher _notificationPublisher;

    public Worker(
        ILogger<Worker> logger,
        IConnector connector,
        MockSourceAdapter mockSourceAdapter,
        WebhookSourceAdapter webhookSourceAdapter,
        INotificationPublisher notificationPublisher)
    {
        _logger = logger;
        _connector = connector;
        _mockSourceAdapter = mockSourceAdapter;
        _webhookSourceAdapter = webhookSourceAdapter;
        _notificationPublisher = notificationPublisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Connector Worker started.");

        _connector.Register(_mockSourceAdapter);
        _connector.Register(_webhookSourceAdapter);

        _connector.OnMessage += async envelope =>
        {
            _logger.LogInformation(
                "Normalized message received. Source: {Source}, Type: {Type}, Title: {Title}",
                envelope.Source,
                envelope.Type,
                envelope.Title
            );

            await _notificationPublisher.PublishAsync(envelope, stoppingToken);
        };

        await _connector.StartAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await _connector.StopAsync(stoppingToken);
    }
}