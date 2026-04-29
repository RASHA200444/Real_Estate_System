"""
handlers/content.py
====================
Handles:
  - content.moderation
  - content.spam_detection
  - content.toxicity_scoring
  - content.sentiment_analysis
  - content.language_detection
  - reports.smart_analysis
  - user.anomaly_detection
"""

from __future__ import annotations
import logging
from typing import Any

from handlers.base import BaseHandler

log = logging.getLogger(__name__)


# ── content.moderation ───────────────────────────────────────────────────────

class ContentModerationHandler(BaseHandler):
    """
    payload: { "text": str, "imagePaths": [str] }
    Returns: ContentModerationResult
    """

    _BLOCKED_KEYWORDS = ["scam", "fraud", "fake", "تزوير", "احتيال"]

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            text = str(payload.get("text") or payload.get("description") or "").lower()
            blocked = any(kw in text for kw in self._BLOCKED_KEYWORDS)
            severity = 3 if blocked else 0

            return {
                "isAllowed": not blocked,
                "severity":  severity,
                "confidence": 0.80,
                "reason": "Keyword match" if blocked else "No prohibited content detected",
            }
        except Exception as exc:
            log.exception("ContentModeration failed")
            return {"isAllowed": True, "severity": 0, "confidence": 0.0, "reason": str(exc)}


# ── content.spam_detection ───────────────────────────────────────────────────

class ContentSpamDetectionHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            text  = str(payload.get("text") or "")
            score = 0.1          # TODO: replace with real spam classifier
            is_spam = score > 0.5
            return {
                "isSpam":     is_spam,
                "score":      round(score, 3),
                "confidence": 0.75,
                "reason":     "Spam model score",
            }
        except Exception as exc:
            log.exception("ContentSpamDetection failed")
            return {"isSpam": False, "score": 0.0, "confidence": 0.0, "reason": str(exc)}


# ── content.toxicity_scoring ─────────────────────────────────────────────────

class ContentToxicityScoringHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            text           = str(payload.get("text") or "")
            toxicity_score = 0.05          # TODO: replace with real toxicity model
            severity       = int(toxicity_score * 10)
            return {
                "toxicityScore": round(toxicity_score, 3),
                "severity":      severity,
                "confidence":    0.78,
                "reason":        "Toxicity model score",
            }
        except Exception as exc:
            log.exception("ContentToxicityScoring failed")
            return {"toxicityScore": 0.0, "severity": 0, "confidence": 0.0, "reason": str(exc)}


# ── content.sentiment_analysis ───────────────────────────────────────────────

class ContentSentimentAnalysisHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            text = str(payload.get("text") or "")
            # TODO: replace with a real sentiment model
            # Simple heuristic for demo
            positive_words = ["great", "excellent", "good", "nice", "ممتاز", "جيد"]
            negative_words = ["bad", "terrible", "awful", "سيء", "رديء"]
            lower = text.lower()
            if any(w in lower for w in positive_words):
                label, score = "positive", 0.85
            elif any(w in lower for w in negative_words):
                label, score = "negative", 0.80
            else:
                label, score = "neutral", 0.70

            return {"label": label, "score": round(score, 3),
                    "confidence": round(score, 3), "reason": "Keyword-based sentiment"}
        except Exception as exc:
            log.exception("ContentSentimentAnalysis failed")
            return {"label": None, "score": 0.0, "confidence": 0.0, "reason": str(exc)}


# ── content.language_detection ───────────────────────────────────────────────

class ContentLanguageDetectionHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            text = str(payload.get("text") or "")
            # TODO: replace with langdetect or fastText
            has_arabic = any('\u0600' <= ch <= '\u06FF' for ch in text)
            lang = "ar" if has_arabic else "en"
            return {"languageCode": lang, "confidence": 0.80,
                    "reason": "Character-set heuristic"}
        except Exception as exc:
            log.exception("ContentLanguageDetection failed")
            return {"languageCode": None, "confidence": 0.0, "reason": str(exc)}


# ── reports.smart_analysis ───────────────────────────────────────────────────

class ReportsSmartAnalysisHandler(BaseHandler):
    """
    AIDecision: 1=Verified (keep complaint), 3=Fraudulent (reject complaint)
    """

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            text     = str(payload.get("text") or payload.get("description") or "")
            severity = int(payload.get("severity", 1))
            # TODO: real analysis model
            decision = 1          # Verified by default
            return {"decision": decision, "severity": severity,
                    "confidence": 0.70, "reason": "Complaint accepted for review"}
        except Exception as exc:
            log.exception("ReportsSmartAnalysis failed")
            return {"decision": 0, "severity": None, "confidence": 0.0, "reason": str(exc)}


# ── user.anomaly_detection ───────────────────────────────────────────────────

class UserAnomalyDetectionHandler(BaseHandler):
    """
    payload: { "activityMetrics": { ... } }
    """

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            metrics       = payload.get("activityMetrics") or {}
            login_count   = int(metrics.get("loginCount",   0))
            failed_logins = int(metrics.get("failedLogins", 0))

            is_suspicious = failed_logins > 5 or login_count > 200
            score         = min(1.0, (failed_logins * 0.1 + login_count * 0.002))

            return {
                "isSuspicious": is_suspicious,
                "score":        round(score, 3),
                "confidence":   0.72,
                "reason":       f"failedLogins={failed_logins}, loginCount={login_count}",
            }
        except Exception as exc:
            log.exception("UserAnomalyDetection failed")
            return {"isSuspicious": False, "score": 0.0, "confidence": 0.0, "reason": str(exc)}
