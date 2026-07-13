using System.Net.Http.Json;

Console.WriteLine("Notification Simulator started.");

var simulatorTarget = Environment.GetEnvironmentVariable("SIMULATOR_TARGET")
    ?? "backend";

var backendUrl = Environment.GetEnvironmentVariable("BACKEND_URL")
    ?? "http://localhost:5199";

var webhookUrl = Environment.GetEnvironmentVariable("WEBHOOK_URL")
    ?? "http://localhost:7071/webhook/notifications";

var notificationsEndpoint = simulatorTarget.Equals("webhook", StringComparison.OrdinalIgnoreCase)
    ? webhookUrl
    : $"{backendUrl}/api/notifications";

using var httpClient = new HttpClient();

Console.WriteLine("Simulator target:");
Console.WriteLine(simulatorTarget);

Console.WriteLine("Target endpoint:");
Console.WriteLine(notificationsEndpoint);

var timestamp = DateTimeOffset.UtcNow;
var timestampText = timestamp.ToString("yyyyMMddHHmmss");

var duplicateKey = $"duplicate-scenario-{timestampText}";

var normalInfoMessage = new NotificationMessage(
    Source: simulatorTarget.Equals("webhook", StringComparison.OrdinalIgnoreCase)
        ? "simulator-webhook"
        : "simulator",
    Type: "info",
    Title: "Bilgilendirme bildirimi",
    Message: "Simulator tarafından oluşturulan normal bilgilendirme mesajıdır.",
    DeduplicationKey: $"info-{timestampText}",
    CreatedAt: timestamp
);

var warningMessage = new NotificationMessage(
    Source: simulatorTarget.Equals("webhook", StringComparison.OrdinalIgnoreCase)
        ? "simulator-webhook"
        : "simulator",
    Type: "warning",
    Title: "Uyarı bildirimi",
    Message: "Simulator tarafından oluşturulan uyarı seviyesindeki mesajdır.",
    DeduplicationKey: $"warning-{timestampText}",
    CreatedAt: timestamp
);

var errorMessage = new NotificationMessage(
    Source: simulatorTarget.Equals("webhook", StringComparison.OrdinalIgnoreCase)
        ? "simulator-webhook"
        : "simulator",
    Type: "error",
    Title: "Hata bildirimi",
    Message: "Simulator tarafından oluşturulan hata seviyesindeki mesajdır.",
    DeduplicationKey: $"error-{timestampText}",
    CreatedAt: timestamp
);

var firstDuplicateMessage = new NotificationMessage(
    Source: simulatorTarget.Equals("webhook", StringComparison.OrdinalIgnoreCase)
        ? "simulator-webhook"
        : "simulator",
    Type: "info",
    Title: "Duplicate test bildirimi",
    Message: "Bu mesaj duplicate senaryosunun ilk gönderimidir.",
    DeduplicationKey: duplicateKey,
    CreatedAt: timestamp
);

var secondDuplicateMessage = new NotificationMessage(
    Source: simulatorTarget.Equals("webhook", StringComparison.OrdinalIgnoreCase)
        ? "simulator-webhook"
        : "simulator",
    Type: "info",
    Title: "Duplicate test bildirimi",
    Message: "Bu mesaj aynı deduplication key ile tekrar gönderilmiştir.",
    DeduplicationKey: duplicateKey,
    CreatedAt: timestamp
);

var malformedMessage = new
{
    source = simulatorTarget.Equals("webhook", StringComparison.OrdinalIgnoreCase)
        ? "simulator-webhook"
        : "simulator",
    type = "warning",
    title = "Bozuk mesaj testi",
    deduplicationKey = $"malformed-{timestampText}",
    createdAt = timestamp
};

await SendNotificationAsync(httpClient, notificationsEndpoint, "Normal info scenario", normalInfoMessage);
await WaitAsync();

await SendNotificationAsync(httpClient, notificationsEndpoint, "Warning scenario", warningMessage);
await WaitAsync();

await SendNotificationAsync(httpClient, notificationsEndpoint, "Error scenario", errorMessage);
await WaitAsync();

await SendNotificationAsync(httpClient, notificationsEndpoint, "Duplicate scenario - first message", firstDuplicateMessage);
await WaitAsync();

await SendNotificationAsync(httpClient, notificationsEndpoint, "Duplicate scenario - repeated message", secondDuplicateMessage);
await WaitAsync();

await SendNotificationAsync(httpClient, notificationsEndpoint, "Malformed message scenario", malformedMessage);

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