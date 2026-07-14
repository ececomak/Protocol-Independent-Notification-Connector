using System.Net;
using System.Net.WebSockets;
using System.Text;
using NotificationConnector.Core;
using NotificationConnector.Models;

namespace NotificationConnector.Adapters;

public class WebSocketSourceAdapter : ISourceAdapter
{
    private readonly ILogger<WebSocketSourceAdapter> _logger;
    private HttpListener? _listener;
    private Task? _listenerTask;

    public WebSocketSourceAdapter(ILogger<WebSocketSourceAdapter> logger)
    {
        _logger = logger;
    }

    public string Name => "websocket-adapter";

    public event Func<RawMessage, Task>? OnRawMessage;

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        var listenUrl = Environment.GetEnvironmentVariable("WEBSOCKET_LISTEN_URL")
            ?? "http://localhost:7072/";

        if (!listenUrl.EndsWith("/"))
        {
            listenUrl += "/";
        }

        _listener = new HttpListener();
        _listener.Prefixes.Add(listenUrl);
        _listener.Start();

        _logger.LogInformation("WebSocket source adapter listening on {ListenUrl}", listenUrl);

        _listenerTask = Task.Run(
            () => ListenAsync(cancellationToken),
            cancellationToken
        );

        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WebSocket source adapter disconnected.");

        if (_listener is not null)
        {
            _listener.Stop();
            _listener.Close();
        }

        return Task.CompletedTask;
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        if (_listener is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();

                _ = Task.Run(
                    () => HandleRequestAsync(context, cancellationToken),
                    cancellationToken
                );
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while listening for WebSocket requests.");
            }
        }
    }

    private async Task HandleRequestAsync(
        HttpListenerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Request.Url?.AbsolutePath != "/ws/notifications")
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
            return;
        }

        if (!context.Request.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        WebSocket? webSocket = null;

        try
        {
            var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
            webSocket = webSocketContext.WebSocket;

            _logger.LogInformation("WebSocket client connected.");

            await ReceiveMessagesAsync(webSocket, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket request could not be processed.");
        }
        finally
        {
            if (webSocket is not null)
            {
                webSocket.Dispose();
            }

            _logger.LogInformation("WebSocket client disconnected.");
        }
    }

    private async Task ReceiveMessagesAsync(
        WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        while (
            webSocket.State == WebSocketState.Open &&
            !cancellationToken.IsCancellationRequested
        )
        {
            var messageBuilder = new StringBuilder();
            WebSocketReceiveResult result;

            do
            {
                result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken
                );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing",
                        cancellationToken
                    );

                    return;
                }

                var messagePart = Encoding.UTF8.GetString(buffer, 0, result.Count);
                messageBuilder.Append(messagePart);
            }
            while (!result.EndOfMessage);

            var payload = messageBuilder.ToString();

            if (string.IsNullOrWhiteSpace(payload))
            {
                continue;
            }

            var rawMessage = new RawMessage(
                AdapterName: Name,
                Payload: payload,
                ReceivedAt: DateTimeOffset.UtcNow
            );

            _logger.LogInformation("WebSocket source adapter received a raw message.");

            if (OnRawMessage is not null)
            {
                await OnRawMessage.Invoke(rawMessage);
            }
        }
    }
}