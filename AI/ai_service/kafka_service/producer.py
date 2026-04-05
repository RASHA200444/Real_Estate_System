from kafka import KafkaProducer
from ai_service.config import KAFKA_BOOTSTRAP_SERVERS
# التعديل هنا: استورد الاتنين مع بعض من المسار الصح بتاعك
from ai_service.ai_utils.json_utils import serialize, deserialize 

class AiKafkaProducer:
    def __init__(self):
        self.producer = KafkaProducer(
            bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
            # دلوقتي بايثون هيعرف مين هي serialize لأننا عملنا لها import فوق
            value_serializer=lambda v: serialize(v)
        )

    def send(self, topic, message):
        self.producer.send(topic, message)
        self.producer.flush()