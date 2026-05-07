namespace OrderManagement.Features.Messaging;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;


public sealed class RabbitMqService(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqService> logger) : IRabbitMqService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Task PublishAsync<TMessage>(
        string queueName,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = options.Value;

            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var json = JsonSerializer.Serialize(message, JsonOptions);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Type = typeof(TMessage).Name;
            properties.MessageId = Guid.NewGuid().ToString();

            channel.BasicPublish(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: body);

            logger.LogInformation(
                "Message {MessageType} published to RabbitMQ queue {QueueName}.",
                typeof(TMessage).Name,
                queueName);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish message {MessageType} to RabbitMQ queue {QueueName}.",
                typeof(TMessage).Name,
                queueName);
        }

        return Task.CompletedTask;
    }
}