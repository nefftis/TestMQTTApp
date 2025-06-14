using Confluent.Kafka;
using System;
using System.Threading.Tasks;

public class KafkaProducer
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _topic;

    public KafkaProducer(string bootstrapServers, string topic)
    {
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<Null, string>(config).Build();
        _topic = topic;
    }

    public void Send(string message)
    {

        _producer.Produce(_topic, new Message<Null, string> { Value = message },
            (deliveryReport) =>
            {
                if (deliveryReport.Error.IsError)
                    Console.WriteLine($"Kafka Ошибка: {deliveryReport.Error.Reason}");
            });

    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(2));
        _producer.Dispose();
    }
}
