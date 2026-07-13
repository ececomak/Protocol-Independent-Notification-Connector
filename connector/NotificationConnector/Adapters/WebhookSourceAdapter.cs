using System.Net;
using System.Text;
using NotificationConnector.Core;
using NotificationConnector.Models;

namespace NotificationConnector.Adapters;

public class WebhookSourceAdapter : ISourceAdapter
{
    private readonly ILogger<WebhookSourceAdapter> _logger;
    private HttpListener? _listener;
    private Task? _listenerTask;

    public WebhookSourceAdapter(ILogger<WebhookSourceAdapter> logger)
    {
        _logger = logger;
    }

    public string Name => "webhook-adapter";

    public event Func<RawMessage, Task>? OnRawMessage;

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        var listenUrl = Environment.GetEnvironmentVariable("WEBHOOK_LISTEN_URL")
            ?? "http://localhost:7071/";

        if (!listenUrl.EndsWith("/"))
        {
            listenUrl += "/";
        }

        _listener = new HttpListener();
        _listener.Prefixes.Add(listenUrl);
        _listener.Start();

        _logger.LogInformation("Webhook source adapter listening on {ListenUrl}", listenUrl);

        _listenerTask = Task.Run(
            () => ListenAsync(cancellationToken),
            cancellationToken
        );

        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Webhook source adapter disconnected.");

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
                _logger.LogError(ex, "Unexpected error while listening for webhook requests.");
            }
        }
    }

    private async Task HandleRequestAsync(
        HttpListenerContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            if (context.Request.HttpMethod != "POST")
            {
                await WriteResponseAsync(
                    context,
                    statusCode: 405,
                    body: "{\"message\":\"Only POST method is allowed.\"}",
                    cancellationToken
                );

                return;
            }

            if (context.Request.Url?.AbsolutePath != "/webhook/notifications")
            {
                await WriteResponseAsync(
                    context,
                    statusCode: 404,
                    body: "{\"message\":\"Webhook endpoint not found.\"}",
                    cancellationToken
                );

                return;
            }

            using var reader = new StreamReader(
                context.Request.InputStream,
                context.Request.ContentEncoding
            );

            var payload = await reader.ReadToEndAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(payload))
            {
                await WriteResponseAsync(
                    context,
                    statusCode: 400,
                    body: "{\"message\":\"Request body is required.\"}",
                    cancellationToken
                );

                return;
            }

            var rawMessage = new RawMessage(
                AdapterName: Name,
                Payload: payload,
                ReceivedAt: DateTimeOffset.UtcNow
            );

            _logger.LogInformation("Webhook source adapter received a raw message.");

            if (OnRawMessage is not null)
            {
                await OnRawMessage.Invoke(rawMessage);
            }

            await WriteResponseAsync(
                context,
                statusCode: 202,
                body: "{\"message\":\"Webhook message accepted.\"}",
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook request could not be processed.");

            await WriteResponseAsync(
                context,
                statusCode: 500,
                body: "{\"message\":\"Webhook request could not be processed.\"}",
                cancellationToken
            );
        }
    }

    private static async Task WriteResponseAsync(
        HttpListenerContext context,
        int statusCode,
        string body,
        CancellationToken cancellationToken)
    {
        var buffer = Encoding.UTF8.GetBytes(body);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = buffer.Length;

        await context.Response.OutputStream.WriteAsync(buffer, cancellationToken);
        context.Response.OutputStream.Close();
    }
}