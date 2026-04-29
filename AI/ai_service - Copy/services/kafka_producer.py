"""
services/kafka_producer.py
===========================
Thin wrapper around kafka-python KafkaProducer.
Serialises the result envelope to JSON and sends it to ai.results.
"""

from __future__ import annotations
import json
import logging
from kafka import KafkaProducer as _KafkaProducer

from config import settings as cfg
from models.envelopes import AiResultEnvelope

log = logging.getLogger(__name__)


class ResultProducer:
    def __init__(self) -> None:
        self._producer = _KafkaProducer(
            bootstrap_servers=cfg.KAFKA_BOOTSTRAP_SERVERS,
            value_serializer=lambda v: json.dumps(v).encode("utf-8"),
            key_serializer=lambda k: k.encode("utf-8") if k else None,
            acks="all",
        )

    def send(self, result: AiResultEnvelope) -> None:
        topic = cfg.KAFKA_TOPIC_RESULTS
        data  = result.to_dict()
        # Use requestId as Kafka message key for ordered delivery per request
        self._producer.send(topic, key=result.requestId, value=data)
        self._producer.flush()
        log.debug("Sent result [%s] → %s", result.requestType, topic)

    def close(self) -> None:
        self._producer.close()
