namespace NotificationConnector.Models;

public record RawMessage(
    string AdapterName,
    string Payload,
    DateTimeOffset ReceivedAt
);