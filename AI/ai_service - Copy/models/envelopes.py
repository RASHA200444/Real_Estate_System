"""
models/envelopes.py
===================
Python dataclasses that mirror the .NET AiRequestEnvelope
and AiResultEnvelope exactly — same field names (camelCase
in JSON, snake_case in Python).
"""

from __future__ import annotations
from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Any


@dataclass
class AiEntityRef:
    type: str          # e.g. "post", "user", "proposal"
    id: int


@dataclass
class AiRequestEnvelope:
    requestId:    str
    requestType:  str
    entity:       AiEntityRef
    payload:      dict[str, Any]
    createdAtUtc: str = ""

    @staticmethod
    def from_dict(d: dict) -> "AiRequestEnvelope":
        entity_raw = d.get("entity") or {}
        return AiRequestEnvelope(
            requestId=d.get("requestId", ""),
            requestType=d.get("requestType", ""),
            entity=AiEntityRef(
                type=entity_raw.get("type", ""),
                id=int(entity_raw.get("id", 0)),
            ),
            payload=d.get("payload") or {},
            createdAtUtc=d.get("createdAtUtc", ""),
        )


@dataclass
class AiResultEnvelope:
    requestId:      str
    requestType:    str
    entity:         AiEntityRef
    payload:        dict[str, Any]
    processedAtUtc: str = field(
        default_factory=lambda: datetime.now(timezone.utc).isoformat()
    )

    def to_dict(self) -> dict:
        return {
            "requestId":      self.requestId,
            "requestType":    self.requestType,
            "entity":         {"type": self.entity.type, "id": self.entity.id},
            "payload":        self.payload,
            "processedAtUtc": self.processedAtUtc,
        }
