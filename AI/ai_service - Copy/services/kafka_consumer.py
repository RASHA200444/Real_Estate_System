"""
services/kafka_consumer.py
===========================
Subscribes to ai.requests, deserialises AiRequestEnvelope,
dispatches to the correct handler, and publishes AiResultEnvelope
back on ai.results.
"""

from __future__ import annotations
import json
import logging
from kafka import KafkaConsumer as _KafkaConsumer

from config import settings as cfg
from models.envelopes import AiRequestEnvelope, AiResultEnvelope
from models.request_types import AiRequestTypes
from services.kafka_producer import ResultProducer
from handlers.dispatcher import HandlerDispatcher

log = logging.getLogger(__name__)


class AiRequestConsumer:
    def __init__(self) -> None:
        self._dispatcher = HandlerDispatcher()
        self._producer   = ResultProducer()

    def start(self) -> None:
        consumer = _KafkaConsumer(
            cfg.KAFKA_TOPIC_REQUESTS,
            bootstrap_servers=cfg.KAFKA_BOOTSTRAP_SERVERS,
            group_id=cfg.KAFKA_GROUP_ID,
            auto_offset_reset=cfg.KAFKA_AUTO_OFFSET_RESET,
            enable_auto_commit=False,           # manual commit — same as .NET side
            value_deserializer=lambda m: json.loads(m.decode("utf-8")),
        )

        log.info("Listening on topic: %s", cfg.KAFKA_TOPIC_REQUESTS)

        for message in consumer:
            try:
                raw      = message.value
                envelope = AiRequestEnvelope.from_dict(raw)

                log.info(
                    "Received [%s] entity=%s/%s requestId=%s",
                    envelope.requestType,
                    envelope.entity.type,
                    envelope.entity.id,
                    envelope.requestId,
                )

                payload = self._dispatcher.dispatch(envelope)

                result = AiResultEnvelope(
                    requestId=envelope.requestId,
                    requestType=envelope.requestType,
                    entity=envelope.entity,
                    payload=payload,
                )
                self._producer.send(result)
                consumer.commit()

            except Exception as exc:
                # Log but never crash the loop — mirrors .NET consumer behaviour
                log.exception("Error processing message: %s", exc)
