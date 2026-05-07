namespace OrderManagement.Features.Messaging;

public interface IRabbitMqService
{
    Task PublishAsync<TMessage>(
        string queueName,
        TMessage message,
        CancellationToken cancellationToken = default);
}