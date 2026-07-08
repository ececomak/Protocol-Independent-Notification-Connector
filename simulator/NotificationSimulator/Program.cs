using System.Net.Http.Json;

Console.WriteLine("Notification Simulator started.");

var backendUrl = "http://localhost:5199";
var notificationsEndpoint = $"{backendUrl}/api/notifications";

using var httpClient = new HttpClient();

Console.WriteLine("Backend URL:");
Console.WriteLine(backendUrl);

var duplicateKey = $"duplicate-scenario-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

var normalInfoMessage = new NotificationMessage(
    Source: "simulator",
    Type: "info",
    Title: "Bilgilendirme bildirimi",
    Message: "Simulator tarafından oluşturulan normal bilgilendirme mesajıdır.",
    DeduplicationKey: $"info-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
    CreatedAt: DateTimeOffset.UtcNow
);

var warningMessage = new NotificationMessage(
    Source: "simulator",
    Type: "warning",
    Title: "Uyarı bildirimi",
    Message: "Simulator tarafından oluşturulan uyarı seviyesindeki mesajdır.",
    DeduplicationKey: $"warning-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
    CreatedAt: DateTimeOffset.UtcNow
);

var errorMessage = new NotificationMessage(
    Source: "simulator",
    Type: "error",
    Title: "Hata bildirimi",
    Message: "Simulator tarafından oluşturulan hata seviyesindeki mesajdır.",
    DeduplicationKey: $"error-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
    CreatedAt: DateTimeOffset.UtcNow
);

var firstDuplicateMessage = new NotificationMessage(
    Source: "simulator",
    Type: "info",
    Title: "Duplicate test bildirimi",
    Message: "Bu mesaj duplicate senaryosunun ilk gönderimidir.",
    DeduplicationKey: duplicateKey,
    CreatedAt: DateTimeOffset.UtcNow
);

var secondDuplicateMessage = new NotificationMessage(
    Source: "simulator",
    Type: "info",
    Title: "Duplicate test bildirimi",
    Message: "Bu mesaj aynı deduplication key ile tekrar gönderilmiştir.",
    DeduplicationKey: duplicateKey,
    CreatedAt: DateTimeOffset.UtcNow
);

var malformedMessage = new
{
    source = "simulator",
    type = "warning",
    title = "Bozuk mesaj testi",
    deduplicationKey = $"malformed-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
    createdAt = DateTimeOffset.UtcNow
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