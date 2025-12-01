using Confluent.Kafka;
using Newtonsoft.Json;

namespace otherServices.Services
{
    public class EmailKafkaProducerService
    {

        private readonly IProducer<Null, string> _producer;

        public EmailKafkaProducerService()
        {
            var config = new ProducerConfig { BootstrapServers = "localhost:9092" };
            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task SendMessageToKafkaAsync(object messageObj)
        {
            var messageJson = JsonConvert.SerializeObject(messageObj);
            await _producer.ProduceAsync("WelcomeEmail", new Message<Null, string> { Value = messageJson });
        }
    }
}
