using Microsoft.Extensions.Options;
using NotificationConnector.Adapters;
using NotificationConnector.Core;
using NotificationConnector.Options;
using NotificationConnector.Publishers;

namespace NotificationConnector;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConnector _connector;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ConnectorOptions _connectorOptions;
    private readonly IReadOnlyDictionary<string, ISourceAdapter> _availableAdapters;

    public Worker(
        ILogger<Worker> logger,
        IConnector connector,
        INotificationPublisher notificationPublisher,
        IOptions<ConnectorOptions> connectorOptions,
        MockSourceAdapter mockSourceAdapter,
        WebhookSourceAdapter webhookSourceAdapter,
        WebSocketSourceAdapter webSocketSourceAdapter)
    {
        _logger = logger;
        _connector = connector;
        _notificationPublisher = notificationPublisher;
        _connectorOptions = connectorOptions.Value;

        _availableAdapters = new Dictionary<string, ISourceAdapter>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            ["mock"] = mockSourceAdapter,
            ["webhook"] = webhookSourceAdapter,
            ["websocket"] = webSocketSourceAdapter
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Connector Worker started.");

        RegisterEnabledAdapters();

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

    private void RegisterEnabledAdapters()
    {
        if (_connectorOptions.EnabledAdapters.Count == 0)
        {
            _logger.LogWarning("No source adapters enabled in configuration.");
            return;
        }

        foreach (var adapterName in _connectorOptions.EnabledAdapters)
        {
            if (!_availableAdapters.TryGetValue(adapterName, out var adapter))
            {
                _logger.LogWarning(
                    "Configured adapter '{AdapterName}' is not available and will be ignored.",
                    adapterName
                );

                continue;
            }

            _connector.Register(adapter);

            _logger.LogInformation(
                "Source adapter registered from configuration: {AdapterName}",
                adapterName
            );
        }
    }
}