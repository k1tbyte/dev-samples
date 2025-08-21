using System.Reflection;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace AccessRefresh.Services.Domain;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class KafkaMessageAttribute(string topic, string messageKey, string? messageType = null) : Attribute
{
    public string MessageKey { get; } = messageKey;
    public string? MessageType { get; } = messageType;
    public string Topic { get; } = topic;
}

public static class KafkaTopic
{
    public const string Notifications = "notifications";
}

public sealed class KafkaService(IConfiguration config, ILogger<KafkaService> logger)
{
    private readonly IProducer<string, string> _producer = new ProducerBuilder<string, string>(
        new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            ClientId = config["Kafka:ClientId"],
            Acks = Acks.All,            // Waiting for confirmation from all replicas
            EnableIdempotence = true,   // Preventing duplication
            MessageTimeoutMs = 30000,
            RequestTimeoutMs = 30000
        }
    ).SetErrorHandler(
        (_, error) => logger.LogError("Kafka error: {Error}", error.Reason)
    ).Build();

    public async Task<bool> ProduceAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        var typeAttribute = typeof(T).GetCustomAttribute<KafkaMessageAttribute>(false);

        if (string.IsNullOrEmpty(typeAttribute?.MessageKey))
        {
            throw new ArgumentException(
                $"The type {typeof(T).Name} must have a BrokerMessage attribute with a valid message key.",
                nameof(T)
            );
        }
        
        try
        {
            var kafkaMessage = new Message<string, string>
            {
                Key = typeAttribute.MessageKey,
                Value = JsonSerializer.Serialize(message, JsonSerializerOptions.Web),
            };
            
            if( typeAttribute.MessageType is not null)
            {
                kafkaMessage.Headers = new Headers
                {
                    { "type", Encoding.UTF8.GetBytes(typeAttribute.MessageType) }
                };
            }

            await _producer.ProduceAsync(typeAttribute.Topic, kafkaMessage, cancellationToken);
            return true;
        }
        catch (ProduceException<string, string> e)
        {
            logger.LogError("Delivery failed: {reason}", e.Error.Reason);
            return false;
        }
    }
}