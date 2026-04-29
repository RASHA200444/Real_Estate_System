"""
services/model_registry.py
===========================
Loads every model/encoder exactly ONCE at startup and exposes
them as a singleton.  Import `registry` anywhere you need a model.
"""

from __future__ import annotations
import logging
import joblib
import faiss
import pandas as pd
from sentence_transformers import SentenceTransformer

from config import settings as cfg

log = logging.getLogger(__name__)


class ModelRegistry:
    """Holds references to all loaded models."""

    def __init__(self) -> None:
        log.info("Loading models — this may take a moment...")

        # ── Prediction models ────────────────────────────────
        self.price_model    = joblib.load(cfg.PRICE_PREDICTOR_PATH)
        self.price_features = joblib.load(cfg.PRICE_PREDICTOR_FEATURES_PATH)
        self.deal_model     = joblib.load(cfg.DEAL_CLASSIFIER_PATH)
        self.area_model     = joblib.load(cfg.AREA_RATER_PATH)
        self.area_stats     = joblib.load(cfg.AREA_STATS_TABLE_PATH)

        # price_range_classifier is optional (not used in current handlers
        # but loaded so future handlers can access it)
        try:
            self.price_range_model   = joblib.load(cfg.PRICE_RANGE_CLASSIFIER_PATH)
            self.price_range_encoder = joblib.load(cfg.PRICE_RANGE_ENCODER_PATH)
        except Exception as exc:
            log.warning("price_range_classifier not loaded: %s", exc)
            self.price_range_model   = None
            self.price_range_encoder = None

        # ── Encoders ─────────────────────────────────────────
        self.type_encoder    = joblib.load(cfg.TYPE_ENCODER_PATH)
        self.city_encoder    = joblib.load(cfg.CITY_ENCODER_PATH)
        self.numeric_scaler  = joblib.load(cfg.NUMERIC_SCALER_PATH)

        # ── FAISS + listing texts ────────────────────────────
        self.faiss_index   = faiss.read_index(str(cfg.FAISS_INDEX_PATH))
        self.listing_texts = pd.read_csv(cfg.LISTING_TEXTS_PATH)

        # ── Sentence transformer ─────────────────────────────
        self.nlp_model = SentenceTransformer(cfg.SENTENCE_TRANSFORMER_MODEL)

        log.info("All models loaded successfully.")


# ── Singleton ────────────────────────────────────────────────
registry = ModelRegistry()
