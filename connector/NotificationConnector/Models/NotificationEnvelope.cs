namespace NotificationConnector.Models;

public record NotificationEnvelope(
    string Source,
    string Type,
    string Title,
    string Message,
    string DeduplicationKey,
    DateTimeOffset CreatedAt
);