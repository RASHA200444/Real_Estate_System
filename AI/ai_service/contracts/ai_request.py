from dataclasses import dataclass
from typing import Any, Dict


@dataclass
class AiRequestMessage:
    requestId: str
    requestType: str
    entityId: int
    payload: Dict[str, Any]