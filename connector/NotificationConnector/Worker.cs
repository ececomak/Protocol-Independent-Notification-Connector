using NotificationConnector.Core;

namespace NotificationConnector;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConnector _connector;

    public Worker(ILogger<Worker> logger, IConnector connector)
    {
        _logger = logger;
        _connector = connector;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Connector Worker started.");

        _connector.OnMessage += async envelope =>
        {
            _logger.LogInformation(
                "Normalized message received. Source: {Source}, Type: {Type}, Title: {Title}",
                envelope.Source,
                envelope.Type,
                envelope.Title
            );

            await Task.CompletedTask;
        };

        await _connector.StartAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await _connector.StopAsync(stoppingToken);
    }
}