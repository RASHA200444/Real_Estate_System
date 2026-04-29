"""
handlers/search.py
==================
Handles:
  - search.similar_listings   → SearchSimilarListingsResult
  - search.semantic_ranking   → SearchSemanticRankingResult
  - search.query_understanding → SearchQueryUnderstandingResult
  - reco.related_posts        → RecoRelatedPostsResult
  - reco.personalized_feed    → RecoPersonalizedFeedResult
  - reco.user_to_user_match   → RecoUserToUserMatchResult
"""

from __future__ import annotations
import json
import logging
from typing import Any

import faiss
import numpy as np

from handlers.base import BaseHandler
from services.model_registry import registry

log = logging.getLogger(__name__)


def _nlp_search(query: str, top_k: int) -> list[dict]:
    q_vec = registry.nlp_model.encode([query]).astype("float32")
    faiss.normalize_L2(q_vec)
    distances, indices = registry.faiss_index.search(q_vec, top_k)
    results = []
    for rank, (idx, score) in enumerate(zip(indices[0], distances[0])):
        results.append({
            "rank":        rank + 1,
            "listingIdx":  int(idx),
            "description": registry.listing_texts.iloc[idx]["listing_text"],
            "score":       round(float(score), 3),
        })
    return results


# ── search.similar_listings ──────────────────────────────────────────────────

class SearchSimilarListingsHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            query  = str(payload.get("query", ""))
            top_k  = int(payload.get("top_k", 5))
            hits   = _nlp_search(query, top_k)
            ids    = [h["listingIdx"] for h in hits]
            return {
                "similarPostIds": ids,
                "confidence":     round(float(hits[0]["score"]), 3) if hits else 0.0,
                "reason":         f"Top {len(ids)} semantic matches for query",
            }
        except Exception as exc:
            log.exception("SearchSimilarListings failed")
            return {"similarPostIds": [], "confidence": 0.0, "reason": str(exc)}


# ── search.semantic_ranking ──────────────────────────────────────────────────

class SearchSemanticRankingHandler(BaseHandler):
    """
    Re-ranks a supplied list of post IDs by semantic similarity to the query.
    payload: { "query": str, "postIds": [long, ...] }
    """

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            query    = str(payload.get("query", ""))
            post_ids = [int(x) for x in payload.get("postIds", [])]
            top_k    = min(len(post_ids), int(payload.get("top_k", len(post_ids))))

            if not post_ids:
                return {"rankedPostIds": [], "confidence": 0.0, "reason": "No postIds provided"}

            hits = _nlp_search(query, top_k)
            # Return only IDs that were in the original candidate list
            candidate_set = set(post_ids)
            ranked = [h["listingIdx"] for h in hits if h["listingIdx"] in candidate_set]

            return {
                "rankedPostIds": ranked,
                "confidence":    round(float(hits[0]["score"]), 3) if hits else 0.0,
                "reason":        f"Re-ranked {len(ranked)} posts",
            }
        except Exception as exc:
            log.exception("SearchSemanticRanking failed")
            return {"rankedPostIds": [], "confidence": 0.0, "reason": str(exc)}


# ── search.query_understanding ───────────────────────────────────────────────

class SearchQueryUnderstandingHandler(BaseHandler):
    """
    Extracts intent and entities from a free-text search query using the NLP model.
    For now: intent is inferred from keywords; entity extraction is keyword-based.
    A future version can swap in a dedicated NER model.
    """

    _INTENT_KEYWORDS = {
        "rent":      ["rent", "rental", "إيجار"],
        "buy":       ["buy", "purchase", "sale", "شراء", "بيع"],
        "invest":    ["invest", "investment", "استثمار"],
    }

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            query = str(payload.get("query", "")).lower()
            intent = "search"
            for k, keywords in self._INTENT_KEYWORDS.items():
                if any(kw in query for kw in keywords):
                    intent = k
                    break

            # Very simple entity extraction — replace with NER model if available
            entities: dict[str, Any] = {}
            if any(w.isdigit() for w in query.split()):
                digits = [w for w in query.split() if w.isdigit()]
                entities["numbers"] = digits

            return {
                "intent":       intent,
                "entitiesJson": json.dumps(entities, ensure_ascii=False),
                "confidence":   0.70,
                "reason":       f"Keyword-based intent detection: '{intent}'",
            }
        except Exception as exc:
            log.exception("SearchQueryUnderstanding failed")
            return {"intent": None, "entitiesJson": None, "confidence": 0.0, "reason": str(exc)}


# ── reco.related_posts ───────────────────────────────────────────────────────

class RecoRelatedPostsHandler(BaseHandler):
    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            query = str(payload.get("description", payload.get("query", "")))
            top_k = int(payload.get("top_k", 5))
            hits  = _nlp_search(query, top_k)
            return {
                "relatedPostIds": [h["listingIdx"] for h in hits],
                "confidence":     round(float(hits[0]["score"]), 3) if hits else 0.0,
                "reason":         f"Semantic similarity to post {entity_id}",
            }
        except Exception as exc:
            log.exception("RecoRelatedPosts failed")
            return {"relatedPostIds": [], "confidence": 0.0, "reason": str(exc)}


# ── reco.personalized_feed ───────────────────────────────────────────────────

class RecoPersonalizedFeedHandler(BaseHandler):
    """
    Uses user's search history / preferred attributes as a query.
    payload: { "preferences": str, "top_k": int }
    """

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        try:
            query = str(payload.get("preferences", "apartment Cairo 2 bedrooms"))
            top_k = int(payload.get("top_k", 10))
            hits  = _nlp_search(query, top_k)
            return {
                "recommendedPostIds": [h["listingIdx"] for h in hits],
                "confidence":         round(float(hits[0]["score"]), 3) if hits else 0.0,
                "reason":             f"Personalised feed for user {entity_id}",
            }
        except Exception as exc:
            log.exception("RecoPersonalizedFeed failed")
            return {"recommendedPostIds": [], "confidence": 0.0, "reason": str(exc)}


# ── reco.user_to_user_match ──────────────────────────────────────────────────

class RecoUserToUserMatchHandler(BaseHandler):
    """
    Placeholder: returns empty until a user-embedding model is available.
    """

    def handle(self, payload: dict[str, Any], entity_type: str, entity_id: int) -> dict[str, Any]:
        return {
            "matchedUserIds": [],
            "confidence":     0.0,
            "reason":         "User-to-user matching model not yet available",
        }
