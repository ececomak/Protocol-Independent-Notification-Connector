using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

Console.WriteLine("Notification Simulator started.");

var simulatorTarget = Environment.GetEnvironmentVariable("SIMULATOR_TARGET")
    ?? "backend";

var backendUrl = Environment.GetEnvironmentVariable("BACKEND_URL")
    ?? "http://localhost:5199";

var webhookUrl = Environment.GetEnvironmentVariable("WEBHOOK_URL")
    ?? "http://localhost:7071/webhook/notifications";

var websocketUrl = Environment.GetEnvironmentVariable("WEBSOCKET_URL")
    ?? "ws://localhost:7072/ws/notifications";

var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
    ?? "localhost";

var rabbitMqPortText = Environment.GetEnvironmentVariable("RABBITMQ_PORT")
    ?? "5672";

var rabbitMqUser = Environment.GetEnvironmentVariable("RABBITMQ_USER")
    ?? "guest";

var rabbitMqPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
    ?? "guest";

var rabbitMqQueue = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE")
    ?? "notifications";

using var httpClient = new HttpClient();

Console.WriteLine("Simulator target:");
Console.WriteLine(simulatorTarget);

var timestamp = DateTimeOffset.UtcNow;
var timestampText = timestamp.ToString("yyyyMMddHHmmss");

var sourceName = simulatorTarget.ToLowerInvariant() switch
{
    "webhook" => "simulator-webhook",
    "websocket" => "simulator-websocket",
    "rabbitmq" => "simulator-rabbitmq",
    _ => "simulator"
};

var duplicateKey = $"duplicate-scenario-{timestampText}";

var normalInfoMessage = new NotificationMessage(
    Source: sourceName,
    Type: "info",
    Title: "Bilgilendirme bildirimi",
    Message: "Simulator tarafından oluşturulan normal bilgilendirme mesajıdır.",
    DeduplicationKey: $"info-{timestampText}",
    CreatedAt: timestamp
);

var warningMessage = new NotificationMessage(
    Source: sourceName,
    Type: "warning",
    Title: "Uyarı bildirimi",
    Message: "Simulator tarafından oluşturulan uyarı seviyesindeki mesajdır.",
    DeduplicationKey: $"warning-{timestampText}",
    CreatedAt: timestamp
);

var errorMessage = new NotificationMessage(
    Source: sourceName,
    Type: "error",
    Title: "Hata bildirimi",
    Message: "Simulator tarafından oluşturulan hata seviyesindeki mesajdır.",
    DeduplicationKey: $"error-{timestampText}",
    CreatedAt: timestamp
);

var firstDuplicateMessage = new NotificationMessage(
    Source: sourceName,
    Type: "info",
    Title: "Duplicate test bildirimi",
    Message: "Bu mesaj duplicate senaryosunun ilk gönderimidir.",
    DeduplicationKey: duplicateKey,
    CreatedAt: timestamp
);

var secondDuplicateMessage = new NotificationMessage(
    Source: sourceName,
    Type: "info",
    Title: "Duplicate test bildirimi",
    Message: "Bu mesaj aynı deduplication key ile tekrar gönderilmiştir.",
    DeduplicationKey: duplicateKey,
    CreatedAt: timestamp
);

var malformedMessage = new
{
    source = sourceName,
    type = "warning",
    title = "Bozuk mesaj testi",
    deduplicationKey = $"malformed-{timestampText}",
    createdAt = timestamp
};

var messages = new (string ScenarioName, object Payload)[]
{
    ("Normal info scenario", normalInfoMessage),
    ("Warning scenario", warningMessage),
    ("Error scenario", errorMessage),
    ("Duplicate scenario - first message", firstDuplicateMessage),
    ("Duplicate scenario - repeated message", secondDuplicateMessage),
    ("Malformed message scenario", malformedMessage)
};

if (simulatorTarget.Equals("websocket", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("Target endpoint:");
    Console.WriteLine(websocketUrl);

    await SendMessagesToWebSocketAsync(websocketUrl, messages);
}
else if (simulatorTarget.Equals("rabbitmq", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("Target RabbitMQ queue:");
    Console.WriteLine($"{rabbitMqHost}:{rabbitMqPortText} / {rabbitMqQueue}");

    await SendMessagesToRabbitMqAsync(
        rabbitMqHost,
        rabbitMqPortText,
        rabbitMqUser,
        rabbitMqPassword,
        rabbitMqQueue,
        messages
    );
}
else
{
    var notificationsEndpoint = simulatorTarget.Equals("webhook", StringComparison.OrdinalIgnoreCase)
        ? webhookUrl
        : $"{backendUrl}/api/notifications";

    Console.WriteLine("Target endpoint:");
    Console.WriteLine(notificationsEndpoint);

    foreach (var message in messages)
    {
        await SendNotificationAsync(
            httpClient,
            notificationsEndpoint,
            message.ScenarioName,
            message.Payload
        );

        await WaitAsync();
    }
}

Console.WriteLine();
Console.WriteLine("Notification Simulator finished.");

static async Task SendNotificationAsync(
    HttpClient httpClient,
    string notificationsEndpoint,
    string scenarioName,
    object notification)
{
    Console.WriteLine();
    Console.WriteLine($"Scenario: {scenarioName}");
    Console.WriteLine("Sending notification...");

    try
    {
        var response = await httpClient.PostAsJsonAsync(notificationsEndpoint, notification);
        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Status Code: {(int)response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Result: Message sent successfully.");
        }
        else
        {
            Console.WriteLine("Result: Message was rejected or ignored.");
        }

        Console.WriteLine("Response:");
        Console.WriteLine(responseBody);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine("Target endpoint could not be reached.");
        Console.WriteLine(ex.Message);
    }
}

static async Task SendMessagesToWebSocketAsync(
    string websocketUrl,
    IEnumerable<(string ScenarioName, object Payload)> messages)
{
    using var webSocket = new ClientWebSocket();

    try
    {
        Console.WriteLine("Connecting to WebSocket endpoint...");
        await webSocket.ConnectAsync(new Uri(websocketUrl), CancellationToken.None);
        Console.WriteLine("WebSocket connection established.");

        foreach (var message in messages)
        {
            Console.WriteLine();
            Console.WriteLine($"Scenario: {message.ScenarioName}");
            Console.WriteLine("Sending WebSocket message...");

            var json = JsonSerializer.Serialize(message.Payload);
            var bytes = Encoding.UTF8.GetBytes(json);

            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None
            );

            Console.WriteLine("Result: WebSocket message sent.");

            await WaitAsync();
        }

        await webSocket.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            "Simulator finished.",
            CancellationToken.None
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine("WebSocket endpoint could not be reached.");
        Console.WriteLine(ex.Message);
    }
}

static async Task SendMessagesToRabbitMqAsync(
    string hostName,
    string portText,
    string userName,
    string password,
    string queueName,
    IEnumerable<(string ScenarioName, object Payload)> messages)
{
    try
    {
        var port = int.Parse(portText);

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        foreach (var message in messages)
        {
            Console.WriteLine();
            Console.WriteLine($"Scenario: {message.ScenarioName}");
            Console.WriteLine("Publishing RabbitMQ message...");

            var json = JsonSerializer.Serialize(message.Payload);
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                body: body
            );

            Console.WriteLine("Result: RabbitMQ message published.");

            await WaitAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("RabbitMQ target could not be reached.");
        Console.WriteLine(ex.Message);
    }
}

static async Task WaitAsync()
{
    await Task.Delay(1000);
}

public record NotificationMessage(
    string Source,
    string Type,
    string Title,
    string Message,
    string DeduplicationKey,
    DateTimeOffset CreatedAt
);