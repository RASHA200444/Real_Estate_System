"""
AI Service — Entry Point
========================
Starts the Kafka consumer loop that listens on ai.requests
and dispatches to the correct handler based on RequestType.
"""

import logging
from services.kafka_consumer import AiRequestConsumer

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s — %(message)s",
)

if __name__ == "__main__":
    logging.info("Starting AI Service...")
    consumer = AiRequestConsumer()
    consumer.start()
