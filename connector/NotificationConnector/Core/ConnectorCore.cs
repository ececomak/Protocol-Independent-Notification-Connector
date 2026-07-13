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

        if (envelope is null)
        {
            Console.WriteLine(
                $"Invalid raw message ignored. Adapter: {rawMessage.AdapterName}, Payload: {rawMessage.Payload}"
            );

            return;
        }

        if (OnMessage is not null)
        {
            await OnMessage.Invoke(envelope);
        }
    }

    private static NotificationEnvelope? Normalize(RawMessage rawMessage)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var payload = JsonSerializer.Deserialize<IncomingNotificationPayload>(
                rawMessage.Payload,
                options
            );

            if (payload is null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(payload.Source) ||
                string.IsNullOrWhiteSpace(payload.Type) ||
                string.IsNullOrWhiteSpace(payload.Title) ||
                string.IsNullOrWhiteSpace(payload.Message))
            {
                return null;
            }

            var source = payload.Source.Trim();
            var type = payload.Type.Trim();
            var title = payload.Title.Trim();
            var message = payload.Message.Trim();

            var deduplicationKey = string.IsNullOrWhiteSpace(payload.DeduplicationKey)
                ? GenerateDeduplicationKey(source, type, title, message)
                : payload.DeduplicationKey.Trim();

            var createdAt = payload.CreatedAt ?? rawMessage.ReceivedAt;

            return new NotificationEnvelope(
                Source: source,
                Type: type,
                Title: title,
                Message: message,
                DeduplicationKey: deduplicationKey,
                CreatedAt: createdAt
            );
        }
        catch
        {
            return null;
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

    private record IncomingNotificationPayload(
        string? Source,
        string? Type,
        string? Title,
        string? Message,
        string? DeduplicationKey,
        DateTimeOffset? CreatedAt
    );
}