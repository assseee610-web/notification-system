namespace NotificationSystem.Web.Interfaces;

public interface IRabbitMQService
{
    void PublishMessage(string queueName, object message);
    string GetQueueName(string notificationType);
}