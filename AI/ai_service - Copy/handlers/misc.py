"""
handlers/misc.py
================
Handles the remaining request types:
  - buyer.installment_risk
  - buyer.rent_eligibility
  - buyer.offer_ranking
  - payment.fraud_detection
  - support.*
  - insights.*
  - contract.*
  - image.quality_scoring
  - decision.engine
"""

from __future__ import annotations
import json
import logging
from typing import Any

from handlers.base import BaseHandler
from services.model_registry import registry
from utils.feature_encoder import build_feature_df
import numpy as np

log = logging.getLogger(__name__)


# ── buyer.installment_risk ────────────────────────────────────────────────────

class BuyerInstallmentRiskHandler(BaseHandler):
    """
    AIInstallmentDecision: 0=NotEvaluated, 1=Able, 2=NotAble
    payload: { "monthlyIncome": float, "monthlyInstallment": float, "existingDebts": float }
    """

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            income       = float(payload.get("monthlyIncome",      0))
            installment  = float(payload.get("monthlyInstallment",  0))
            existing     = float(payload.get("existingDebts",       0))

            if income <= 0:
                return {"isAble": 0, "score": None, "confidence": 0.0,
                        "reason": "Monthly income not provided"}

            dti   = (installment + existing) / income   # Debt-to-income ratio
            score = max(0, round((1 - dti) * 100))
            is_able = 1 if dti <= 0.40 else 2

            return {
                "isAble":     is_able,
                "score":      score,
                "confidence": 0.78,
                "reason":     f"DTI={dti:.2%} — {'within' if is_able == 1 else 'exceeds'} 40 % threshold",
            }
        except Exception as exc:
            log.exception("BuyerInstallmentRisk failed")
            return {"isAble": 0, "score": None, "confidence": 0.0, "reason": str(exc)}


# ── buyer.rent_eligibility ────────────────────────────────────────────────────

class BuyerRentEligibilityHandler(BaseHandler):
    """
    AIRentDecision: 0=NotEvaluated, 1=Eligible, 2=NotEligible
    payload: { "monthlyIncome": float, "monthlyRent": float }
    """

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            income = float(payload.get("monthlyIncome", 0))
            rent   = float(payload.get("monthlyRent",   0))

            if income <= 0:
                return {"isAble": 0, "score": None, "confidence": 0.0,
                        "reason": "Monthly income not provided"}

            ratio   = rent / income
            score   = max(0, round((1 - ratio) * 100))
            is_able = 1 if ratio <= 0.33 else 2

            return {
                "isAble":     is_able,
                "score":      score,
                "confidence": 0.76,
                "reason":     f"Rent/income ratio={ratio:.2%} — {'within' if is_able == 1 else 'exceeds'} 33 % threshold",
            }
        except Exception as exc:
            log.exception("BuyerRentEligibility failed")
            return {"isAble": 0, "score": None, "confidence": 0.0, "reason": str(exc)}


# ── buyer.offer_ranking ───────────────────────────────────────────────────────

class BuyerOfferRankingHandler(BaseHandler):
    """
    payload: { "proposals": [ { "proposalId": long, "offeredPrice": float, ... } ] }
    """

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            proposals = payload.get("proposals") or []
            if not proposals:
                return {"postId": entity_id, "rankedProposalIds": [],
                        "confidence": 0.0, "reason": "No proposals to rank"}

            # Simple rank by offered price descending
            ranked = sorted(proposals, key=lambda p: float(p.get("offeredPrice", 0)), reverse=True)
            ranked_ids = [int(p["proposalId"]) for p in ranked if "proposalId" in p]

            return {
                "postId":            entity_id,
                "rankedProposalIds": ranked_ids,
                "rankedJson":        json.dumps(ranked_ids),
                "confidence":        0.75,
                "reason":            f"Ranked {len(ranked_ids)} proposals by offered price",
            }
        except Exception as exc:
            log.exception("BuyerOfferRanking failed")
            return {"postId": entity_id, "rankedProposalIds": [],
                    "confidence": 0.0, "reason": str(exc)}


# ── payment.fraud_detection ───────────────────────────────────────────────────

class PaymentFraudDetectionHandler(BaseHandler):
    """
    AIDecision: 1=Verified, 3=Fraudulent
    payload: { "amount": float, "ipCountry": str, "cardBin": str, ... }
    """

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            amount     = float(payload.get("amount", 0))
            ip_country = str(payload.get("ipCountry", "EG"))

            fraud_score = 10
            if amount > 1_000_000:
                fraud_score += 30
            if ip_country not in ("EG", "SA", "AE", "KW"):
                fraud_score += 25

            decision = 3 if fraud_score >= 50 else 1
            return {
                "decision":   decision,
                "score":      fraud_score,
                "confidence": 0.80,
                "reason":     f"Fraud score: {fraud_score}",
            }
        except Exception as exc:
            log.exception("PaymentFraudDetection failed")
            return {"decision": 0, "score": None, "confidence": 0.0, "reason": str(exc)}


# ── image.quality_scoring ─────────────────────────────────────────────────────

class ImageQualityScoringHandler(BaseHandler):
    """
    payload: { "imagePaths": [str, ...] }
    """

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            paths = payload.get("imagePaths") or payload.get("image_paths") or []
            if not paths:
                return {"postId": entity_id, "overallScore": 0.0,
                        "confidence": 0.0, "reason": "No images provided"}
            # TODO: replace with real image-quality model
            overall = 0.75
            return {
                "postId":              entity_id,
                "overallScore":        overall,
                "perImageScoresJson":  json.dumps([{"path": p, "score": 0.75} for p in paths]),
                "issuesJson":          None,
                "confidence":          0.70,
                "reason":              f"Scored {len(paths)} image(s)",
            }
        except Exception as exc:
            log.exception("ImageQualityScoring failed")
            return {"postId": entity_id, "overallScore": 0.0,
                    "confidence": 0.0, "reason": str(exc)}


# ── decision.engine ───────────────────────────────────────────────────────────

class DecisionEngineHandler(BaseHandler):
    """
    Aggregates multiple AI signals and returns a final post status suggestion.
    PostPendingStatus values: "Accepted", "Refused", "Pending"
    payload: { "signals": { "fakeProperty": int, "imageManipulation": int,
                             "postDocument": int, "priceEvaluation": int,
                             "contentModeration": bool } }
    """

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            signals = payload.get("signals") or {}

            # AIDecision.Fraudulent == 3
            is_fraudulent = (
                signals.get("fakeProperty",      0) == 3 or
                signals.get("imageManipulation", 0) == 3 or
                signals.get("postDocument",      0) == 3
            )
            content_blocked = signals.get("contentModeration", True) is False

            # PriceEvaluation.VeryHigh==3, VeryLow==5
            price_suspicious = signals.get("priceEvaluation", 0) in (3, 5)

            if is_fraudulent or content_blocked:
                status, risk = "Refused", 90
            elif price_suspicious:
                status, risk = "Pending", 50
            else:
                status, risk = "Accepted", 10

            return {
                "suggestedPendingStatus": status,
                "riskLevel":              risk,
                "confidence":             0.85,
                "reason":                 f"DecisionEngine → {status} (risk={risk})",
            }
        except Exception as exc:
            log.exception("DecisionEngine failed")
            return {"suggestedPendingStatus": "Pending", "riskLevel": None,
                    "confidence": 0.0, "reason": str(exc)}


# ── support.* ────────────────────────────────────────────────────────────────

class SupportAutoReplySuggestionHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        return {"suggestedReply": "Thank you for contacting support. We will get back to you shortly.",
                "confidence": 0.65, "reason": "Default template"}


class SupportTicketClassificationHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        return {"label": "general_inquiry", "severity": 1,
                "confidence": 0.60, "reason": "Default classification"}


class SupportPriorityScoringHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        return {"priority": 2, "confidence": 0.65, "reason": "Medium priority by default"}


# ── insights.* ───────────────────────────────────────────────────────────────

class InsightsMarketTrendsHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        return {"reportJson": json.dumps({"status": "market_trends_pending"}),
                "confidence": 0.0, "reason": "Market trends model not yet available"}


class InsightsDemandPredictionHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        return {"demandJson": json.dumps({"status": "demand_prediction_pending"}),
                "confidence": 0.0, "reason": "Demand prediction model not yet available"}


class InsightsUserBehaviorSummaryHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        return {"summaryJson": json.dumps({"status": "behavior_summary_pending"}),
                "confidence": 0.0, "reason": "User behaviour model not yet available"}


# ── contract.* ───────────────────────────────────────────────────────────────

class ContractRiskFlagsHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        return {"riskFlagsJson": json.dumps([]), "severity": 0,
                "confidence": 0.0, "reason": "Contract risk model not yet available"}


class ContractClauseSuggestionHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        return {"suggestedClausesJson": json.dumps([]),
                "confidence": 0.0, "reason": "Contract clause model not yet available"}
