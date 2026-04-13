#ai_result.py
from dataclasses import dataclass
from typing import Any, Dict


@dataclass
class AiResultMessage:
    requestId: str
    requestType: str
    result: Dict[str, Any]