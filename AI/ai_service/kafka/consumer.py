from kafka_service.consumer import AiKafkaConsumer
from kafka_service.producer import AiKafkaProducer
from config import KAFKA_BOOTSTRAP_SERVERS, CONSUMER_GROUP
from ai_utils.json_utils import deserialize


class AiKafkaConsumer:

    def __init__(self, topic):

        self.consumer = KafkaConsumer(
            topic,
            bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
            group_id=CONSUMER_GROUP,
            auto_offset_reset="earliest",
            value_deserializer=lambda v: deserialize(v)
        )

    def listen(self):
        for message in self.consumer:
            yield message.value