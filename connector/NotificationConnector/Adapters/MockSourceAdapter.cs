using System.Text.Json;
using NotificationConnector.Core;
using NotificationConnector.Models;

namespace NotificationConnector.Adapters;

public class MockSourceAdapter : ISourceAdapter
{
    private readonly ILogger<MockSourceAdapter> _logger;

    public MockSourceAdapter(ILogger<MockSourceAdapter> logger)
    {
        _logger = logger;
    }

    public string Name => "mock-adapter";

    public event Func<RawMessage, Task>? OnRawMessage;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mock source adapter connected.");

        await Task.Delay(1000, cancellationToken);

        var payload = JsonSerializer.Serialize(new
        {
            source = "mock-adapter",
            type = "info",
            title = "Connector test bildirimi",
            message = "Bu bildirim mock adapter üzerinden connector core tarafından normalize edilmiştir."
        });

        var rawMessage = new RawMessage(
            AdapterName: Name,
            Payload: payload,
            ReceivedAt: DateTimeOffset.UtcNow
        );

        _logger.LogInformation("Mock source adapter produced a raw message.");

        if (OnRawMessage is not null)
        {
            await OnRawMessage.Invoke(rawMessage);
        }
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mock source adapter disconnected.");

        return Task.CompletedTask;
    }
}