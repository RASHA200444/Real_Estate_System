"""
config/settings.py
==================
All configuration values in one place.
Override any value via environment variables or a .env file.
"""

import os
from pathlib import Path
from dotenv import load_dotenv

load_dotenv()

# ── Kafka ────────────────────────────────────────────────────
KAFKA_BOOTSTRAP_SERVERS: str = os.getenv("KAFKA_BOOTSTRAP_SERVERS", "localhost:9092")
KAFKA_GROUP_ID: str          = os.getenv("KAFKA_GROUP_ID",          "ai-service-group")
KAFKA_TOPIC_REQUESTS: str    = os.getenv("KAFKA_TOPIC_REQUESTS",    "ai.requests")
KAFKA_TOPIC_RESULTS: str     = os.getenv("KAFKA_TOPIC_RESULTS",     "ai.results")
KAFKA_AUTO_OFFSET_RESET: str = os.getenv("KAFKA_AUTO_OFFSET_RESET", "earliest")

# ── Model paths ──────────────────────────────────────────────
_BASE = Path(os.getenv("MODELS_BASE_PATH", r"D:\year4\GP\Real_Estate_System\AI\models"))

SAVED_PATH:    Path = _BASE / "saved"
ENCODERS_PATH: Path = _BASE / "encoders"

# Saved models
PRICE_PREDICTOR_PATH:          Path = SAVED_PATH / "price_predictor.pkl"
PRICE_PREDICTOR_FEATURES_PATH: Path = SAVED_PATH / "price_predictor_features.pkl"
PRICE_RANGE_CLASSIFIER_PATH:   Path = SAVED_PATH / "price_range_classifier.pkl"
DEAL_CLASSIFIER_PATH:          Path = SAVED_PATH / "deal_classifier.pkl"
AREA_RATER_PATH:               Path = SAVED_PATH / "area_rater.pkl"
AREA_STATS_TABLE_PATH:         Path = SAVED_PATH / "area_stats_table.pkl"
FAISS_INDEX_PATH:              Path = SAVED_PATH / "listing_search.faiss"
LISTING_TEXTS_PATH:            Path = SAVED_PATH / "listing_texts.csv"

# Encoders
TYPE_ENCODER_PATH:        Path = ENCODERS_PATH / "Type_encoder.pkl"
CITY_ENCODER_PATH:        Path = ENCODERS_PATH / "City_encoder.pkl"
PRICE_RANGE_ENCODER_PATH: Path = ENCODERS_PATH / "Price_Range_encoder.pkl"
NUMERIC_SCALER_PATH:      Path = ENCODERS_PATH / "numeric_scaler.pkl"

# ── NLP model ────────────────────────────────────────────────
SENTENCE_TRANSFORMER_MODEL: str = os.getenv(
    "SENTENCE_TRANSFORMER_MODEL", "all-MiniLM-L6-v2"
)
