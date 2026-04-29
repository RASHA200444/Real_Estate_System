"""
utils/feature_encoder.py
=========================
Shared helper that converts a raw payload dict into the feature
dict expected by the ML models.  Centralised here so every handler
uses identical feature engineering.
"""

from __future__ import annotations
from typing import Any
import pandas as pd

from services.model_registry import registry


def encode_input(data: dict[str, Any]) -> dict[str, float]:
    """
    Encode raw request payload into model-ready features.

    Parameters mirror the keys the .NET backend sends inside
    AiRequestEnvelope.Payload for property-related requests.
    """
    try:
        type_enc = float(
            registry.type_encoder.transform([str(data.get("type", "Apartment"))])[0]
        )
    except Exception:
        type_enc = 0.0

    try:
        city_enc = float(
            registry.city_encoder.transform([str(data.get("city", "Cairo"))])[0]
        )
    except Exception:
        city_enc = 0.0

    return {
        "Area":                  float(data.get("area",                 100)),
        "Bedrooms":              float(data.get("bedrooms",             2)),
        "Bathrooms":             float(data.get("bathrooms",            1)),
        "Level":                 float(data.get("level",                1)),
        "City_enc":              city_enc,
        "is_furnished":          1 if str(data.get("furnished",         "no")).lower()        == "yes"   else 0,
        "is_ready":              1 if str(data.get("delivery_term",     "ready")).lower()     == "ready" else 0,
        "is_cash":               1 if str(data.get("payment",           "cash")).lower()      == "cash"  else 0,
        "months_until_delivery": float(data.get("months_until_delivery", 0)),
        "compound_price_mean":   float(data.get("compound_price_mean",   2_500_000)),
        "Type_enc":              type_enc,
    }


def build_feature_df(data: dict[str, Any]) -> pd.DataFrame:
    """Return a single-row DataFrame with only the columns the price model needs."""
    features = encode_input(data)
    return pd.DataFrame([features])[registry.price_features]
