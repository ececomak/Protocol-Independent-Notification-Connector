using System.Net;
using System.Net.Http.Json;
using NotificationConnector.Models;

namespace NotificationConnector.Publishers;

public class BackendNotificationPublisher
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

    public async Task<bool> TryPublishAsync(
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

                return true;
            }

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
            {
                _logger.LogWarning(
                    "Backend rejected notification as handled. StatusCode: {StatusCode}, Response: {Response}",
                    (int)response.StatusCode,
                    responseBody
                );

                return true;
            }

            _logger.LogWarning(
                "Backend could not process notification. It will be retried. StatusCode: {StatusCode}, Response: {Response}",
                (int)response.StatusCode,
                responseBody
            );

            return false;
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Backend API could not be reached. Notification will be retried."
            );

            return false;
        }
    }
}