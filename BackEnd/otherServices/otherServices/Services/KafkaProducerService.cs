
using Confluent.Kafka;
using Newtonsoft.Json;

namespace otherServices.Data_Project.service
{

    public class KafkaProducerService
    {
        private readonly IProducer<Null, string> _producer;

        public KafkaProducerService()
        {
            var config = new ProducerConfig { BootstrapServers = "localhost:9092" };
            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task SendMessageToKafkaAsync(object messageObj)
        {
            var messageJson = JsonConvert.SerializeObject(messageObj);
            await _producer.ProduceAsync("receivedMessages", new Message<Null, string> { Value = messageJson });
        }
    }
}
