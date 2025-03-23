using Confluent.Kafka;
using System;
using System.Threading.Tasks;

public class KafkaProducerService
{
    private static IProducer<string, string> _producer;

    public static void Initialize(string bootstrapServers, string securityProtocol, string saslMechanism, string saslUsername, string saslPassword)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            SecurityProtocol = Enum.Parse<SecurityProtocol>(securityProtocol),
            SaslMechanism = Enum.Parse<SaslMechanism>(saslMechanism),
            SaslUsername = saslUsername,
            SaslPassword = saslPassword
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public static async Task ProduceAsync(string topic, string message)
    {
        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = message
        });
    }
}