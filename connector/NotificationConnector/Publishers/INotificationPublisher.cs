using NotificationConnector.Models;

namespace NotificationConnector.Publishers;

public interface INotificationPublisher
{
    Task PublishAsync(NotificationEnvelope notification, CancellationToken cancellationToken);
}