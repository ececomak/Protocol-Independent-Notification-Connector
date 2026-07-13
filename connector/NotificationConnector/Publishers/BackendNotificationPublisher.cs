using System.Net.Http.Json;
using NotificationConnector.Models;

namespace NotificationConnector.Publishers;

public class BackendNotificationPublisher : INotificationPublisher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BackendNotificationPublisher> _logger;
    private readonly string _notificationsEndpoint;

    public BackendNotificationPublisher(
        HttpClient httpClient,
        ILogger<BackendNotificationPublisher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var backendUrl = Environment.GetEnvironmentVariable("BACKEND_URL")
            ?? "http://localhost:5199";

        _notificationsEndpoint = $"{backendUrl}/api/notifications";
    }

    public async Task PublishAsync(
        NotificationEnvelope notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                _notificationsEndpoint,
                notification,
                cancellationToken
            );

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Notification published to backend. Source: {Source}, Type: {Type}, Title: {Title}",
                    notification.Source,
                    notification.Type,
                    notification.Title
                );
            }
            else
            {
                _logger.LogWarning(
                    "Backend rejected notification. StatusCode: {StatusCode}, Response: {Response}",
                    (int)response.StatusCode,
                    responseBody
                );
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Backend API could not be reached. Notification was not published."
            );
        }
    }
}