using System.Net.Http.Json;

Console.WriteLine("Notification Simulator started.");

var backendUrl = Environment.GetEnvironmentVariable("BACKEND_URL")
    ?? "http://localhost:5199";

var notificationsEndpoint = $"{backendUrl}/api/notifications";

using var httpClient = new HttpClient();

Console.WriteLine("Backend URL:");
Console.WriteLine(backendUrl);

var timestamp = DateTimeOffset.UtcNow;
var timestampText = timestamp.ToString("yyyyMMddHHmmss");

var duplicateKey = $"duplicate-scenario-{timestampText}";

var normalInfoMessage = new NotificationMessage(
    Source: "simulator",
    Type: "info",
    Title: "Bilgilendirme bildirimi",
    Message: "Simulator tarafından oluşturulan normal bilgilendirme mesajıdır.",
    DeduplicationKey: $"info-{timestampText}",
    CreatedAt: timestamp
);

var warningMessage = new NotificationMessage(
    Source: "simulator",
    Type: "warning",
    Title: "Uyarı bildirimi",
    Message: "Simulator tarafından oluşturulan uyarı seviyesindeki mesajdır.",
    DeduplicationKey: $"warning-{timestampText}",
    CreatedAt: timestamp
);

var errorMessage = new NotificationMessage(
    Source: "simulator",
    Type: "error",
    Title: "Hata bildirimi",
    Message: "Simulator tarafından oluşturulan hata seviyesindeki mesajdır.",
    DeduplicationKey: $"error-{timestampText}",
    CreatedAt: timestamp
);

var firstDuplicateMessage = new NotificationMessage(
    Source: "simulator",
    Type: "info",
    Title: "Duplicate test bildirimi",
    Message: "Bu mesaj duplicate senaryosunun ilk gönderimidir.",
    DeduplicationKey: duplicateKey,
    CreatedAt: timestamp
);

var secondDuplicateMessage = new NotificationMessage(
    Source: "simulator",
    Type: "info",
    Title: "Duplicate test bildirimi",
    Message: "Bu mesaj aynı deduplication key ile tekrar gönderilmiştir.",
    DeduplicationKey: duplicateKey,
    CreatedAt: timestamp
);

var malformedMessage = new
{
    source = "simulator",
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
            Console.WriteLine("Result: Message was rejected or ignored by backend.");
        }

        Console.WriteLine("Backend response:");
        Console.WriteLine(responseBody);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine("Backend API'ye ulaşılamadı.");
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