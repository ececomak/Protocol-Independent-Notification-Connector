using System.Net.Http.Json;

Console.WriteLine("Notification Simulator started.");

var backendUrl = "http://localhost:5199";
var notificationsEndpoint = $"{backendUrl}/api/notifications";

using var httpClient = new HttpClient();

var sampleMessage = new NotificationMessage(
    Source: "simulator",
    Type: "info",
    Title: "Simulator test bildirimi",
    Message: "Bu bildirim .NET simulator tarafından backend API'ye gönderilmiştir.",
    DeduplicationKey: $"simulator-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
    CreatedAt: DateTimeOffset.UtcNow
);

Console.WriteLine("Backend URL:");
Console.WriteLine(backendUrl);

Console.WriteLine("Sending sample message...");
Console.WriteLine($"Title: {sampleMessage.Title}");
Console.WriteLine($"Message: {sampleMessage.Message}");
Console.WriteLine($"Deduplication Key: {sampleMessage.DeduplicationKey}");

try
{
    var response = await httpClient.PostAsJsonAsync(notificationsEndpoint, sampleMessage);
    var responseBody = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine("Message sent successfully.");
        Console.WriteLine("Backend response:");
        Console.WriteLine(responseBody);
    }
    else
    {
        Console.WriteLine($"Message could not be sent. Status code: {(int)response.StatusCode}");
        Console.WriteLine("Backend response:");
        Console.WriteLine(responseBody);
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine("Backend API'ye ulaşılamadı.");
    Console.WriteLine(ex.Message);
}

public record NotificationMessage(
    string Source,
    string Type,
    string Title,
    string Message,
    string DeduplicationKey,
    DateTimeOffset CreatedAt
);