"""
handlers/pricing.py
====================
Handles:
  - price.anomaly_detection   → PriceAnomalyDetectionResult
  - negotiation.price_suggestion (basic predicted price helper)
"""

from __future__ import annotations
import logging
from typing import Any

import numpy as np

from handlers.base import BaseHandler
from services.model_registry import registry
from utils.feature_encoder import build_feature_df

log = logging.getLogger(__name__)


# ── price.anomaly_detection ──────────────────────────────────────────────────

class PriceAnomalyDetectionHandler(BaseHandler):
    """
    Compares listed_price against the predicted price.

    PriceEvaluation enum values (must match C# PriceEvaluation):
      0 = NotEvaluated
      1 = Fair
      2 = High
      3 = VeryHigh
      4 = Low
      5 = VeryLow
    """

    _LABELS = {
        "fair":      1,
        "high":      2,
        "very_high": 3,
        "low":       4,
        "very_low":  5,
    }

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            X          = build_feature_df(payload)
            log_pred   = registry.price_model.predict(X)[0]
            predicted  = float(np.expm1(log_pred))

            listed = payload.get("listed_price")
            if listed is None:
                return {"priceEvaluation": 0, "confidence": 0.0, "reason": "No listed_price provided"}

            ratio = float(listed) / predicted

            if   ratio > 1.30:  label, conf = "very_high", 0.90
            elif ratio > 1.15:  label, conf = "high",      0.80
            elif ratio < 0.70:  label, conf = "very_low",  0.90
            elif ratio < 0.85:  label, conf = "low",       0.80
            else:               label, conf = "fair",      0.85

            return {
                "priceEvaluation": self._LABELS[label],
                "confidence":      round(conf, 3),
                "reason": (
                    f"Listed {listed:,.0f} EGP vs predicted {predicted:,.0f} EGP "
                    f"(ratio={ratio:.2f})"
                ),
            }
        except Exception as exc:
            log.exception("PriceAnomalyDetection failed")
            return {"priceEvaluation": 0, "confidence": 0.0, "reason": str(exc)}


# ── negotiation.price_suggestion ─────────────────────────────────────────────

class NegotiationPriceSuggestionHandler(BaseHandler):
    """
    Returns a suggested offer price and a ready-to-send negotiation message.
    """

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            X         = build_feature_df(payload)
            log_pred  = registry.price_model.predict(X)[0]
            predicted = float(np.expm1(log_pred))
            suggested = round(predicted * 0.92)          # 8 % below predicted as opening offer

            return {
                "suggestedPrice":   suggested,
                "suggestedMessage": (
                    f"Based on current market data, a fair offer for this property "
                    f"would be around {suggested:,.0f} EGP."
                ),
                "confidence": 0.78,
                "reason": f"Predicted market value: {predicted:,.0f} EGP",
            }
        except Exception as exc:
            log.exception("NegotiationPriceSuggestion failed")
            return {"suggestedPrice": None, "suggestedMessage": None, "confidence": 0.0, "reason": str(exc)}


# ── negotiation.counter_offer_suggestion ─────────────────────────────────────

class NegotiationCounterOfferSuggestionHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            X         = build_feature_df(payload)
            log_pred  = registry.price_model.predict(X)[0]
            predicted = float(np.expm1(log_pred))

            buyer_offer = float(payload.get("buyer_offer", predicted * 0.85))
            counter     = round((predicted + buyer_offer) / 2)

            return {
                "counterOfferPrice":   counter,
                "counterOfferMessage": (
                    f"Consider countering at {counter:,.0f} EGP, which reflects "
                    f"the current market value and the buyer's offer."
                ),
                "confidence": 0.75,
                "reason": f"Midpoint between predicted ({predicted:,.0f}) and offer ({buyer_offer:,.0f})",
            }
        except Exception as exc:
            log.exception("NegotiationCounterOffer failed")
            return {"counterOfferPrice": None, "counterOfferMessage": None, "confidence": 0.0, "reason": str(exc)}
