"""
handlers/deal.py
================
Handles:
  - (internal) deal classification used by owner forecast handlers
  - owner.forecast.price
  - owner.forecast.demand
  - owner.forecast.revenue
"""

from __future__ import annotations
import logging
from typing import Any

import numpy as np

from handlers.base import BaseHandler
from services.model_registry import registry
from utils.feature_encoder import build_feature_df

log = logging.getLogger(__name__)

_DEAL_LABELS = ["great_deal", "fair_price", "overpriced"]


def _predict_deal(payload: dict[str, Any]) -> tuple[str, float, dict]:
    X     = build_feature_df(payload)
    pred  = registry.deal_model.predict(X)[0]
    proba = registry.deal_model.predict_proba(X)[0]
    return (
        _DEAL_LABELS[pred],
        round(float(max(proba)) * 100, 1),
        {
            "greatDeal":  round(float(proba[0]) * 100, 1),
            "fairPrice":  round(float(proba[1]) * 100, 1),
            "overpriced": round(float(proba[2]) * 100, 1),
        },
    )


# ── owner.forecast.price ─────────────────────────────────────────────────────

class OwnerForecastPriceHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            X         = build_feature_df(payload)
            log_pred  = registry.price_model.predict(X)[0]
            predicted = float(np.expm1(log_pred))
            low       = round(predicted * 0.85)
            high      = round(predicted * 1.15)

            return {
                "postId":         entity_id,
                "suggestedPrice": round(predicted),
                "priceRangeJson": f'{{"low":{low},"high":{high},"currency":"EGP"}}',
                "confidence":     0.82,
                "reason": f"Model predicted {predicted:,.0f} EGP ±15 %",
            }
        except Exception as exc:
            log.exception("OwnerForecastPrice failed")
            return {"postId": entity_id, "suggestedPrice": None, "priceRangeJson": None,
                    "confidence": 0.0, "reason": str(exc)}


# ── owner.forecast.demand ────────────────────────────────────────────────────

class OwnerForecastDemandHandler(BaseHandler):
    """
    Uses area_rater to infer demand level for the post's city.
    """

    _DEMAND_MAP = {0: "Low", 1: "Medium", 2: "High"}

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            city = str(payload.get("city", ""))
            try:
                city_enc = registry.city_encoder.transform([city])[0]
            except Exception:
                city_enc = 0

            _AREA_FEATURES = [
                "listing_count", "avg_price", "price_std",
                "avg_area", "furnished_rate", "ready_rate", "cash_rate",
            ]
            row = registry.area_stats[registry.area_stats["City_enc"] == city_enc]
            if row.empty:
                return {"postId": entity_id, "demandLevel": "Low",
                        "demandJson": None, "confidence": 0.0,
                        "reason": f"City '{city}' not found in area stats"}

            X    = row[_AREA_FEATURES].fillna(0)
            pred = registry.area_model.predict(X)[0]
            lvl  = self._DEMAND_MAP.get(pred, "Medium")

            return {
                "postId":      entity_id,
                "demandLevel": lvl,
                "demandJson":  f'{{"city":"{city}","areaRating":{pred}}}',
                "confidence":  0.75,
                "reason":      f"Area rater score {pred} for city '{city}'",
            }
        except Exception as exc:
            log.exception("OwnerForecastDemand failed")
            return {"postId": entity_id, "demandLevel": None, "demandJson": None,
                    "confidence": 0.0, "reason": str(exc)}


# ── owner.forecast.revenue ───────────────────────────────────────────────────

class OwnerForecastRevenueHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            X            = build_feature_df(payload)
            log_pred     = registry.price_model.predict(X)[0]
            predicted    = float(np.expm1(log_pred))
            platform_fee = float(payload.get("platform_fee_percent", 1.5)) / 100
            net_revenue  = round(predicted * (1 - platform_fee))

            return {
                "postId":          entity_id,
                "expectedRevenue": net_revenue,
                "revenueJson": (
                    f'{{"grossPrice":{round(predicted)},'
                    f'"platformFeePercent":{platform_fee * 100},'
                    f'"netRevenue":{net_revenue},"currency":"EGP"}}'
                ),
                "confidence": 0.78,
                "reason": f"Predicted price {predicted:,.0f} EGP minus {platform_fee*100:.1f}% fee",
            }
        except Exception as exc:
            log.exception("OwnerForecastRevenue failed")
            return {"postId": entity_id, "expectedRevenue": None, "revenueJson": None,
                    "confidence": 0.0, "reason": str(exc)}
