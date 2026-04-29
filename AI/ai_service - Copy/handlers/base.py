"""
handlers/base.py
================
Abstract base class for all AI handlers.
Each handler receives the request payload and returns the result payload dict.
"""

from __future__ import annotations
from abc import ABC, abstractmethod
from typing import Any


class BaseHandler(ABC):
    @abstractmethod
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        """
        Process the request and return the result payload dict.
        The dict will be serialised into AiResultEnvelope.Payload.
        """
        ...
