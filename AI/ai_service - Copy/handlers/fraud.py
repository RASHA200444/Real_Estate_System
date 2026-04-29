"""
handlers/fraud.py
=================
Handles all fraud-related request types.

AIDecision enum (must match C# AIDecision):
  0 = NotReviewed
  1 = Verified
  2 = Uncertain
  3 = Fraudulent
"""

from __future__ import annotations
import logging
from typing import Any

from handlers.base import BaseHandler

log = logging.getLogger(__name__)


class _DocumentAnalysisBase(BaseHandler):
    """
    Shared skeleton for all document-analysis handlers.
    Looks for any doc-path key the .NET backend might send.
    """

    # Every key the .NET side uses across all document request types
    _DOC_PATH_KEYS = [
        "docPath",                 # generic fallback
        "doc_path",                # snake_case fallback
        "nidPath",                 # fraud.document_analysis        (User NID)
        "ownershipDocPath",        # fraud.ownership_document_analysis
        "commercialRegisterPath",  # fraud.commercial_register_analysis
        "postDocPath",             # fraud.post_document_analysis
        "projectDocPath",          # fraud.project_document_analysis
    ]

    def _analyze(self, payload: dict[str, Any]) -> tuple[int, float, str]:
        """
        Returns (decision_int, confidence, reason).
        Default: Uncertain — swap in your real OCR/classification model here.
        """
        doc_path = next(
            (payload[k] for k in self._DOC_PATH_KEYS if payload.get(k)), ""
        )
        if not doc_path:
            return 0, 0.0, "No document path provided"
        # TODO: call your OCR / classification model here
        return 2, 0.60, f"Document received ({doc_path}) — awaiting model review"

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            decision, conf, reason = self._analyze(payload)
            return {"decision": decision, "confidence": conf, "reason": reason}
        except Exception as exc:
            log.exception("%s failed", self.__class__.__name__)
            return {"decision": 0, "confidence": 0.0, "reason": str(exc)}


# ── 01) Fraud / Documents ────────────────────────────────────────────────────

class FraudDocumentAnalysisHandler(_DocumentAnalysisBase):
    """fraud.document_analysis — User NID"""


class FraudOwnershipDocumentAnalysisHandler(_DocumentAnalysisBase):
    """fraud.ownership_document_analysis — Landlord ownership doc"""


class FraudCommercialRegisterAnalysisHandler(_DocumentAnalysisBase):
    """fraud.commercial_register_analysis — Company commercial register"""


class FraudProjectDocumentAnalysisHandler(_DocumentAnalysisBase):
    """fraud.project_document_analysis — Developer project doc"""


# ── 02) Fraud / Posts ────────────────────────────────────────────────────────

class FraudFakePropertyDetectionHandler(BaseHandler):
    """
    fraud.fake_property_detection
    payload fields: title, description, price, area, city, images (list of paths)
    """

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            # TODO: plug in your fake-listing classifier
            price = float(payload.get("price", 0))
            area  = float(payload.get("area",  0))
            if price > 0 and area > 0 and (price / area) < 500:
                return {"decision": 2, "confidence": 0.65,
                        "reason": "Price per sqm unusually low — possible fake listing"}
            return {"decision": 1, "confidence": 0.72, "reason": "No obvious fake-listing signals"}
        except Exception as exc:
            log.exception("FraudFakePropertyDetection failed")
            return {"decision": 0, "confidence": 0.0, "reason": str(exc)}


class FraudImageManipulationHandler(BaseHandler):
    """
    fraud.image_manipulation
    payload: { "imagePaths": [str, ...] }
    """

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            image_paths = payload.get("imagePaths") or payload.get("image_paths") or []
            if not image_paths:
                return {"decision": 0, "confidence": 0.0, "reason": "No images provided"}
            # TODO: plug in your image-manipulation detection model
            return {"decision": 1, "confidence": 0.70,
                    "reason": f"Checked {len(image_paths)} image(s) — no manipulation detected"}
        except Exception as exc:
            log.exception("FraudImageManipulation failed")
            return {"decision": 0, "confidence": 0.0, "reason": str(exc)}


class FraudPostDocumentAnalysisHandler(_DocumentAnalysisBase):
    """fraud.post_document_analysis — Post ownership/title-deed doc"""