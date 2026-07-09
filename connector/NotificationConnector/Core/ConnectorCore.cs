using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NotificationConnector.Models;

namespace NotificationConnector.Core;

public class ConnectorCore : IConnector
{
    private readonly ConcurrentDictionary<string, ISourceAdapter> _adapters = new();

    public event Func<NotificationEnvelope, Task>? OnMessage;

    public void Register(ISourceAdapter adapter)
    {
        if (!_adapters.TryAdd(adapter.Name, adapter))
        {
            throw new InvalidOperationException($"Adapter already registered: {adapter.Name}");
        }

        adapter.OnRawMessage += HandleRawMessageAsync;
    }

    public void Unregister(string adapterName)
    {
        if (_adapters.TryRemove(adapterName, out var adapter))
        {
            adapter.OnRawMessage -= HandleRawMessageAsync;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var adapter in _adapters.Values)
        {
            await adapter.ConnectAsync(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var adapter in _adapters.Values)
        {
            await adapter.DisconnectAsync(cancellationToken);
        }
    }

    private async Task HandleRawMessageAsync(RawMessage rawMessage)
    {
        var envelope = Normalize(rawMessage);

        if (OnMessage is not null)
        {
            await OnMessage.Invoke(envelope);
        }
    }

    private static NotificationEnvelope Normalize(RawMessage rawMessage)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(rawMessage.Payload);

            var source = payload?.GetValueOrDefault("source") ?? rawMessage.AdapterName;
            var type = payload?.GetValueOrDefault("type") ?? "info";
            var title = payload?.GetValueOrDefault("title") ?? "Notification";
            var message = payload?.GetValueOrDefault("message") ?? rawMessage.Payload;

            var deduplicationKey = GenerateDeduplicationKey(source, type, title, message);

            return new NotificationEnvelope(
                Source: source,
                Type: type,
                Title: title,
                Message: message,
                DeduplicationKey: deduplicationKey,
                CreatedAt: rawMessage.ReceivedAt
            );
        }
        catch
        {
            var deduplicationKey = GenerateDeduplicationKey(
                rawMessage.AdapterName,
                "raw",
                "Malformed message",
                rawMessage.Payload
            );

            return new NotificationEnvelope(
                Source: rawMessage.AdapterName,
                Type: "raw",
                Title: "Malformed message",
                Message: rawMessage.Payload,
                DeduplicationKey: deduplicationKey,
                CreatedAt: rawMessage.ReceivedAt
            );
        }
    }

    private static string GenerateDeduplicationKey(
        string source,
        string type,
        string title,
        string message)
    {
        var rawValue = $"{source}|{type}|{title}|{message}";
        var bytes = Encoding.UTF8.GetBytes(rawValue);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}