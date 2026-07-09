using NotificationConnector.Models;

namespace NotificationConnector.Core;

public interface ISourceAdapter
{
    string Name { get; }

    Task ConnectAsync(CancellationToken cancellationToken);

    Task DisconnectAsync(CancellationToken cancellationToken);

    event Func<RawMessage, Task>? OnRawMessage;
}