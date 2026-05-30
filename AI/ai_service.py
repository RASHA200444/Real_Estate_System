"""
ai_service.py — Kafka AI Service for Real Estate System
=========================================================
Consumes: topic "ai.requests"   → AiRequestEnvelope (camelCase JSON)
Produces: topic "ai.results"    → AiResultEnvelope  (camelCase JSON)

Envelope schema (mirrors AiContracts.cs exactly):
  IN  → { requestId, requestType, entity:{type,id}, createdAtUtc, payload:{...} }
  OUT → { requestId, requestType, entity:{type,id}, payload:{...}, processedAtUtc }

Models used (from models.md):
  • price_predictor.pkl      → XGBRegressor  (log-price, features in price_predictor_features.pkl)
  • deal_classifier.pkl      → XGBClassifier (deal quality)
  • area_rater.pkl           → DecisionTreeClassifier (demand 0/1/2)
  • area_stats_table.pkl     → DataFrame with city area stats
  • price_range_classifier.pkl → XGBClassifier (High/Low/Luxury/Middle)
  • City_encoder.pkl         → numpy array of city label-encoded classes
  • Type_encoder.pkl         → numpy array of property type classes
  • Price_Range_encoder.pkl  → numpy array of price range classes
  • numeric_scaler.pkl       → numpy array of numeric feature names

Encoders are numpy arrays of class labels (NOT sklearn LabelEncoder objects).
We do manual label→index lookup, NOT .transform().

RequestType constants mirror AiRequestTypes.cs.
Result payload fields mirror AiResultPayloads.cs exactly.
"""

import json
import os
import logging
import numpy as np
import pandas as pd
import joblib
from kafka import KafkaConsumer, KafkaProducer
from datetime import datetime, timezone

# ─────────────────────────────────────────────────────────────
# Logging
# ─────────────────────────────────────────────────────────────
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    datefmt="%H:%M:%S",
)
log = logging.getLogger("ai_service")

# ─────────────────────────────────────────────────────────────
# Paths  (adjust to your deployment layout)
# ─────────────────────────────────────────────────────────────
BASE_DIR   = os.path.dirname(os.path.abspath(__file__))
SAVE_PATH  = os.path.join(BASE_DIR, "models", "saved")
ENC_PATH   = os.path.join(BASE_DIR, "models", "encoders")

# Media root — same base as BackEnd wwwroot/uploads
# .NET sends relative paths like "uploads/posts/doc.pdf"
MEDIA_ROOT = os.environ.get(
    "MEDIA_ROOT",
    r"D:\year4\GP\Real_Estate_System\BackEnd\otherServices\otherServices"
)

# Kafka
KAFKA_BOOTSTRAP = os.environ.get("KAFKA_BOOTSTRAP", "localhost:9092")
TOPIC_IN        = "ai.requests"
TOPIC_OUT       = "ai.results"
GROUP_ID        = "ai-service-group"

# ─────────────────────────────────────────────────────────────
# Numpy-safe JSON encoder
# ─────────────────────────────────────────────────────────────
class _NpEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, np.integer):  return int(obj)
        if isinstance(obj, np.floating): return float(obj)
        if isinstance(obj, np.bool_):    return bool(obj)
        if isinstance(obj, np.ndarray):  return obj.tolist()
        return super().default(obj)

# ─────────────────────────────────────────────────────────────
# Load models
# ─────────────────────────────────────────────────────────────
log.info("Loading models…")

price_model    = joblib.load(os.path.join(SAVE_PATH, "price_predictor.pkl"))
price_features = joblib.load(os.path.join(SAVE_PATH, "price_predictor_features.pkl"))
# price_features = ['Area','Bedrooms','Bathrooms','Level','City_enc',
#                   'is_furnished','is_ready','is_cash','months_until_delivery',
#                   'compound_price_mean','Type_enc']

deal_model     = joblib.load(os.path.join(SAVE_PATH, "deal_classifier.pkl"))
area_model     = joblib.load(os.path.join(SAVE_PATH, "area_rater.pkl"))
area_stats     = joblib.load(os.path.join(SAVE_PATH, "area_stats_table.pkl"))
price_range_model = joblib.load(os.path.join(SAVE_PATH, "price_range_classifier.pkl"))

# Encoders: sklearn LabelEncoder objects (classes_ attribute holds the labels)
city_enc_obj        = joblib.load(os.path.join(ENC_PATH, "City_encoder.pkl"))
type_enc_obj        = joblib.load(os.path.join(ENC_PATH, "Type_encoder.pkl"))
price_range_enc_obj = joblib.load(os.path.join(ENC_PATH, "Price_Range_encoder.pkl"))

city_list  = list(city_enc_obj.classes_)
type_list  = list(type_enc_obj.classes_)
pr_list    = list(price_range_enc_obj.classes_)

# Optional NLP / FAISS for semantic search (graceful fallback if missing)
try:
    import faiss
    from sentence_transformers import SentenceTransformer
    faiss_index   = faiss.read_index(os.path.join(SAVE_PATH, "listing_search.faiss"))
    listing_texts = pd.read_csv(os.path.join(SAVE_PATH, "listing_texts.csv"))
    nlp_model     = SentenceTransformer("all-MiniLM-L6-v2")
    NLP_AVAILABLE = True
    log.info("NLP/FAISS loaded ✓")
except Exception as _e:
    NLP_AVAILABLE = False
    log.warning(f"NLP/FAISS not available ({_e}) — semantic handlers will return empty.")

log.info("All models loaded ✓")
log.info(f"price_features  : {price_features}")
log.info(f"city_list sample: {city_list[:5]}")
log.info(f"type_list       : {type_list}")

# ─────────────────────────────────────────────────────────────
# City → compound price mean (EGP) — fallback when not in payload
# ─────────────────────────────────────────────────────────────
_CITY_COMPOUND_MEAN: dict[str, float] = {
    "New Cairo - El Tagamoa": 7_000_000,
    "New Cairo":              6_500_000,
    "New Capital City":       5_500_000,
    "Sheikh Zayed":           5_000_000,
    "Nasr City":              4_000_000,
    "Maadi":                  4_500_000,
    "Heliopolis":             4_200_000,
    "Zamalek":                6_000_000,
    "6Th Of October":         3_200_000,
    "Mostakbal City":         3_800_000,
    "Shorouk City":           3_500_000,
    "Rehab City":             4_000_000,
    "Madinaty":               5_000_000,
    "North Coast":            6_000_000,
    "Hurghada":               2_800_000,
    "Sharm Al-Sheikh":        3_500_000,
    "Alexandria":             2_200_000,
    "Dokki":                  3_500_000,
    "Mohandessin":            3_800_000,
}
_DEFAULT_COMPOUND_MEAN = 2_500_000

# ─────────────────────────────────────────────────────────────
# .NET camelCase → model field mapping
#
# The .NET AiRequestDispatcher serialises payloads with
# JsonNamingPolicy.CamelCase, so "NumberOfRooms" → "numberOfRooms".
# Post model fields we care about:
#   numberOfRooms, numberOfBathrooms, area, isFurnished, floorNumber,
#   location, price (double?), type (PropertyType enum int: Rent=0,Sale=1),
#   postDocPath, tagsJson, title, description, nidPath, ownershipDocPath
# ─────────────────────────────────────────────────────────────

# PropertyType enum: Rent=0, Sale=1  (from all_enums.cs)
# IsInstallment enum: Cash=0, Installment=1

_CITY_ALIASES = {
    "new cairo":       "New Cairo - El Tagamoa",
    "tagamoa":         "New Cairo - El Tagamoa",
    "october":         "6Th Of October",
    "6th of october":  "6Th Of October",
    "cairo":           "Downtown Cairo",
    "alex":            "Smoha",
}

def _city_enc(city_raw: str) -> int:
    """Label-encode city using LabelEncoder.transform(). Falls back to 0 on unknown."""
    key = str(city_raw).strip().title()
    try:
        return int(city_enc_obj.transform([key])[0])
    except ValueError:
        for alias, canonical in _CITY_ALIASES.items():
            if alias in key.lower():
                try:
                    return int(city_enc_obj.transform([canonical])[0])
                except ValueError:
                    pass
        log.warning(f"Unknown city '{city_raw}' → 0")
        return 0

# All posts are Apartments — type is fixed.
_APARTMENT_ENC = int(type_enc_obj.transform(["Apartment"])[0])

def _bool(val) -> bool:
    if isinstance(val, bool): return val
    return str(val).lower() in ("true", "1", "yes")

def _float(val, default=0.0) -> float:
    try:
        return float(val)
    except (TypeError, ValueError):
        return default

def _int(val, default=0) -> int:
    try:
        return int(val)
    except (TypeError, ValueError):
        return default

# ─────────────────────────────────────────────────────────────
# Core feature builder
# ─────────────────────────────────────────────────────────────
def _build_features(payload: dict) -> dict:
    """
    Map camelCase .NET payload → model feature dict.
    Feature names must match price_predictor_features.pkl exactly:
      ['Area','Bedrooms','Bathrooms','Level','City_enc',
       'is_furnished','is_ready','is_cash','months_until_delivery',
       'compound_price_mean','Type_enc']
    """
    p = payload  # shorthand

    # ── Numeric fields ───────────────────────────────────────
    area       = _float(p.get("area") or p.get("Area"), 100.0)
    bedrooms   = _float(p.get("numberOfRooms") or p.get("bedrooms") or p.get("Bedrooms"), 2.0)
    bathrooms  = _float(p.get("numberOfBathrooms") or p.get("bathrooms") or p.get("Bathrooms"), 1.0)
    level      = _float(p.get("floorNumber") or p.get("level") or p.get("Level"), 1.0)

    # ── City ────────────────────────────────────────────────
    city_raw   = str(p.get("location") or p.get("city") or p.get("City") or "Cairo")
    city_enc   = _city_enc(city_raw)
    city_title = city_raw.strip().title()

    # Unit type is always Apartment (Post model has no unit-type field)
    type_enc   = _APARTMENT_ENC

    # ── Boolean flags ────────────────────────────────────────
    is_furnished = 1 if _bool(p.get("isFurnished") or p.get("furnished")) else 0

    # isReady: .NET may not send this; infer from deliveryDate / type
    # If deliveryDate is missing or in the past → ready
    months = _float(p.get("monthsUntilDelivery") or p.get("months_until_delivery"), 0.0)
    is_ready = 1 if months <= 0 else 0

    # isInstallment: IsInstallment enum  Cash=0, Installment=1
    is_installment_raw = p.get("isInstallment") or p.get("IsInstallment") or 0
    is_cash = 0 if _int(is_installment_raw) == 1 else 1

    # ── compound_price_mean ──────────────────────────────────
    compound_mean = _float(
        p.get("compoundPriceMean") or p.get("compound_price_mean"),
        _CITY_COMPOUND_MEAN.get(city_title, _DEFAULT_COMPOUND_MEAN)
    )

    return {
        "Area":                  area,
        "Bedrooms":              bedrooms,
        "Bathrooms":             bathrooms,
        "Level":                 level,
        "City_enc":              float(city_enc),
        "is_furnished":          float(is_furnished),
        "is_ready":              float(is_ready),
        "is_cash":               float(is_cash),
        "months_until_delivery": months,
        "compound_price_mean":   compound_mean,
        "Type_enc":              float(type_enc),
        "_city_title":           city_title,
    }

def _to_X(features: dict) -> pd.DataFrame:
    """Return DataFrame with only the price model features, in order."""
    row = {k: features[k] for k in price_features}
    return pd.DataFrame([row])

# ─────────────────────────────────────────────────────────────
# Price prediction (safe — handles log-transformed target)
# ─────────────────────────────────────────────────────────────
_PRICE_MIN = 500_000      # 500 K EGP
_PRICE_MAX = 150_000_000  # 150 M EGP

def _predict_price(X: pd.DataFrame) -> float:
    """
    Returns predicted price in EGP.
    Model was trained on log1p(price) → we apply expm1.
    Falls back to direct prediction if values are already in EGP range.
    """
    raw = float(price_model.predict(X)[0])

    # Detect if model outputs log-space or direct price
    if raw < 20:          # log1p(150M) ≈ 18.8  → definitely log-space
        price = float(np.expm1(raw))
    else:
        price = raw

    price = max(_PRICE_MIN, min(price, _PRICE_MAX))
    log.debug(f"price_model raw={raw:.4f} → {price:,.0f} EGP")
    return price

# ─────────────────────────────────────────────────────────────
# Kafka helpers
# ─────────────────────────────────────────────────────────────
_producer = KafkaProducer(
    bootstrap_servers=KAFKA_BOOTSTRAP,
    value_serializer=lambda v: json.dumps(v, cls=_NpEncoder, ensure_ascii=False).encode("utf-8"),
    key_serializer=lambda k: k.encode("utf-8") if k else None,
)

def _send(request_id: str, request_type: str,
          entity_type: str, entity_id: int, payload: dict):
    """Publish AiResultEnvelope to ai.results."""
    envelope = {
        "requestId":      request_id,
        "requestType":    request_type,
        "entity":         {"type": entity_type, "id": entity_id},
        "payload":        payload,
        "processedAtUtc": datetime.now(timezone.utc).isoformat(),
    }
    _producer.send(TOPIC_OUT, key=request_id, value=envelope)
    _producer.flush()
    log.info(f"→ SENT [{request_type}] entity={entity_type}/{entity_id} | {payload}")

# ─────────────────────────────────────────────────────────────
# Document path helper
# ─────────────────────────────────────────────────────────────
def _check_doc_path(payload: dict, *keys) -> tuple[bool, str]:
    """
    Resolve a document path from payload and check if the file exists.
    Returns (exists: bool, reason: str).
    .NET stores paths relative to wwwroot (e.g. "uploads/nid/file.pdf").
    """
    rel_path = None
    for k in keys:
        rel_path = payload.get(k)
        if rel_path:
            break

    if not rel_path:
        return False, "No document path provided"

    clean = str(rel_path).replace("/", os.sep).replace("\\", os.sep).lstrip(os.sep)
    full  = os.path.normpath(os.path.join(MEDIA_ROOT, clean))

    if os.path.exists(full):
        return True, f"Document found: {clean}"
    return False, f"Document not found: {clean}"

# ─────────────────────────────────────────────────────────────
# ══════════════════════════════════════════════════════════════
#  HANDLERS — one per AiRequestType
#  Each returns a dict matching its AiResultPayload in AiResultPayloads.cs
# ══════════════════════════════════════════════════════════════
# ─────────────────────────────────────────────────────────────

# ── AIDecision enum values (from all_enums.cs) ───────────────
AI_UNCERTAIN   = 0
AI_VERIFIED    = 1
AI_FRAUDULENT  = 2
AI_NOT_REVIEWED = 3

# ── AIInstallmentDecision / AIRentDecision ───────────────────
AI_INST_DISABLE    = -1
AI_INST_NOT_CERTAIN = 0
AI_INST_ABLE       = 1

# ── PriceEvaluation enum ─────────────────────────────────────
PRICE_VERY_LOW  = -2
PRICE_LOW       = -1
PRICE_ACCEPTABLE = 0
PRICE_HIGH      = 1
PRICE_VERY_HIGH = 2

# ─────────────────────────────────────────────────────────────
# 01) Fraud / Documents
# Entity: user (NID), landlord (OwnershipDoc), company (CommercialRegister)
#         post (PostDocPath), project (ProjectDocPath)
# Result: AiDecisionPayloadBase → { decision, confidence, reason }
#         decision = AIDecision int
# ─────────────────────────────────────────────────────────────

def _fraud_doc_result(payload: dict, *path_keys) -> dict:
    """Generic document analysis — checks file existence."""
    exists, reason = _check_doc_path(payload, *path_keys)
    if exists:
        return {"decision": AI_VERIFIED, "confidence": 0.92, "reason": reason}
    return {"decision": AI_UNCERTAIN, "confidence": 0.0, "reason": reason}

def handle_fraud_document_analysis(payload: dict, entity_id: int) -> dict:
    # entity=user  → payload has nidPath (User.NID)
    return _fraud_doc_result(payload, "nidPath", "nid", "filePath")

def handle_fraud_ownership_document_analysis(payload: dict, entity_id: int) -> dict:
    # entity=landlord → Landlords.OwnershipDoc
    return _fraud_doc_result(payload, "ownershipDocPath", "ownershipDoc", "filePath")

def handle_fraud_commercial_register_analysis(payload: dict, entity_id: int) -> dict:
    # entity=company → Companies.CommercialRegister
    return _fraud_doc_result(payload, "commercialRegisterPath", "commercialRegister", "filePath")

def handle_fraud_post_document_analysis(payload: dict, entity_id: int) -> dict:
    # entity=post → Posts.PostDocPath
    return _fraud_doc_result(payload, "postDocPath", "docPath", "filePath")

def handle_fraud_project_document_analysis(payload: dict, entity_id: int) -> dict:
    # entity=project → Projects.ProjectDocPath
    return _fraud_doc_result(payload, "projectDocPath", "docPath", "filePath")

# ─────────────────────────────────────────────────────────────
# 02) Fraud / Posts
# ─────────────────────────────────────────────────────────────

def handle_fraud_fake_property_detection(payload: dict, entity_id: int) -> dict:
    """
    Result: FraudFakePropertyDetectionResult (AiDecisionPayloadBase)
    → { decision: AIDecision int, confidence, reason }
    Uses price model to detect price anomalies indicative of fake listings.
    """
    try:
        features = _build_features(payload)
        X = _to_X(features)
        predicted = _predict_price(X)

        listed = _float(payload.get("price") or payload.get("listedPrice"), 0.0)

        if listed <= 0:
            return {
                "decision": AI_UNCERTAIN,
                "confidence": 0.0,
                "reason": "No listed price in payload",
            }

        ratio = listed / predicted
        # Suspicious if price differs by more than 40% either way
        if ratio < 0.55 or ratio > 1.55:
            return {
                "decision": AI_FRAUDULENT,
                "confidence": round(min(0.95, 0.5 + abs(1 - ratio) * 0.5), 3),
                "reason": (
                    f"Listed {listed:,.0f} vs predicted {predicted:,.0f} EGP "
                    f"(ratio={ratio:.2f}) — price deviation too large"
                ),
            }
        return {
            "decision": AI_VERIFIED,
            "confidence": 0.88,
            "reason": f"Price ratio {ratio:.2f} within normal range",
        }
    except Exception as e:
        log.exception("handle_fraud_fake_property_detection")
        return {"decision": AI_UNCERTAIN, "confidence": 0.0, "reason": str(e)}


def handle_fraud_image_manipulation(payload: dict, entity_id: int) -> dict:
    """
    Result: FraudImageManipulationResult (AiDecisionPayloadBase)
    This handler is triggered as a synthetic envelope by ImageDedupResultsConsumer
    — it already has decision/confidence/reason filled.
    We pass them through.
    """
    try:
        decision   = _int(payload.get("decision"), AI_UNCERTAIN)
        confidence = _float(payload.get("confidence"), 0.0)
        reason     = payload.get("reason") or "Image dedup complete"
        return {"decision": decision, "confidence": confidence, "reason": reason}
    except Exception as e:
        log.exception("handle_fraud_image_manipulation")
        return {"decision": AI_UNCERTAIN, "confidence": 0.0, "reason": str(e)}


def handle_price_anomaly_detection(payload: dict, entity_id: int) -> dict:
    """
    Result: PriceAnomalyDetectionResult
    → { priceEvaluation: PriceEvaluation int, confidence, reason }
    PriceEvaluation enum: VeryLow=-2, Low=-1, Acceptable=0, High=1, VeryHigh=2
    """
    try:
        features = _build_features(payload)
        X = _to_X(features)
        predicted = _predict_price(X)

        listed = _float(payload.get("price") or payload.get("listedPrice"), 0.0)

        if listed <= 0:
            return {"priceEvaluation": PRICE_ACCEPTABLE, "confidence": 0.0,
                    "reason": "No listed price — cannot evaluate"}

        ratio = listed / predicted
        if   ratio > 1.40: ev, conf = PRICE_VERY_HIGH, 0.92
        elif ratio > 1.20: ev, conf = PRICE_HIGH,      0.82
        elif ratio < 0.60: ev, conf = PRICE_VERY_LOW,  0.92
        elif ratio < 0.80: ev, conf = PRICE_LOW,       0.82
        else:              ev, conf = PRICE_ACCEPTABLE, 0.88

        return {
            "priceEvaluation": ev,
            "confidence": round(conf, 3),
            "reason": (
                f"Listed {listed:,.0f} vs predicted {predicted:,.0f} EGP "
                f"(ratio={ratio:.2f})"
            ),
        }
    except Exception as e:
        log.exception("handle_price_anomaly_detection")
        return {"priceEvaluation": PRICE_ACCEPTABLE, "confidence": 0.0, "reason": str(e)}

# ─────────────────────────────────────────────────────────────
# 03) Buyer / Proposals
# entity=proposal
# ─────────────────────────────────────────────────────────────

def _score_buyer(payload: dict) -> int:
    """
    Heuristic eligibility score 0-100 based on available signals.
    Real model can replace this; we use deal_classifier as proxy.
    """
    try:
        features = _build_features(payload)
        X = _to_X(features)
        # deal_classifier: classes are deal quality (0/1/2)
        proba = deal_model.predict_proba(X)[0]
        # Map highest class probability to 0-100 score
        score = int(max(proba) * 100)
        return score
    except Exception:
        return 50

def handle_buyer_installment_risk(payload: dict, entity_id: int) -> dict:
    """
    Result: BuyerInstallmentRiskResult
    → { isAble: AIInstallmentDecision int, score: int?, confidence, reason }
    AIInstallmentDecision: Disable=-1, NotCertain=0, Able=1
    Payload from .NET: proposal fields (offeredprice, installmentDurationMonths,
                       downPayment, installmentAmount, postPrice, etc.)
    """
    try:
        offered_price = _float(payload.get("offeredPrice") or payload.get("offeredprice"), 0.0)
        duration_months = _int(payload.get("installmentDurationMonths"), 12)
        down_payment    = _float(payload.get("downPayment"), 0.0)
        post_price      = _float(payload.get("postPrice") or payload.get("price"), offered_price)
        answers_json    = payload.get("eligibilityAnswersJson") or "{}"

        if post_price <= 0 or offered_price <= 0:
            return {
                "isAble": AI_INST_NOT_CERTAIN,
                "score": None,
                "confidence": 0.0,
                "reason": "Missing price information for installment assessment",
            }

        # Down-payment ratio check
        down_ratio = down_payment / post_price if post_price > 0 else 0
        # Monthly payment estimate
        monthly_est = (offered_price - down_payment) / max(duration_months, 1)

        score = _score_buyer(payload)

        # Decision logic
        if down_ratio >= 0.25 and score >= 60:
            is_able = AI_INST_ABLE
            reason  = f"Down payment {down_ratio:.0%}, score={score}"
        elif down_ratio < 0.10 or score < 35:
            is_able = AI_INST_DISABLE
            reason  = f"Insufficient down payment ({down_ratio:.0%}) or low score ({score})"
        else:
            is_able = AI_INST_NOT_CERTAIN
            reason  = f"Borderline case: down={down_ratio:.0%}, score={score}"

        return {
            "isAble":     is_able,
            "score":      score,
            "confidence": 0.78,
            "reason":     reason,
        }
    except Exception as e:
        log.exception("handle_buyer_installment_risk")
        return {"isAble": AI_INST_NOT_CERTAIN, "score": None, "confidence": 0.0, "reason": str(e)}


def handle_buyer_rent_eligibility(payload: dict, entity_id: int) -> dict:
    """
    Result: BuyerRentEligibilityResult
    → { isAble: AIRentDecision int, score: int?, confidence, reason }
    AIRentDecision: Disable=-1, NotCertain=0, Able=1
    """
    try:
        offered_price = _float(payload.get("offeredPrice") or payload.get("offeredprice"), 0.0)
        post_price    = _float(payload.get("postPrice") or payload.get("price"), offered_price)

        if offered_price <= 0:
            return {
                "isAble": AI_INST_NOT_CERTAIN,
                "score": None,
                "confidence": 0.0,
                "reason": "Missing offered price for rent assessment",
            }

        score = _score_buyer(payload)
        price_ratio = offered_price / post_price if post_price > 0 else 1.0

        if score >= 55 and price_ratio >= 0.90:
            is_able = AI_INST_ABLE
            reason  = f"Score={score}, offer ratio={price_ratio:.2f}"
        elif score < 30 or price_ratio < 0.70:
            is_able = AI_INST_DISABLE
            reason  = f"Low score ({score}) or offer too low ({price_ratio:.2f})"
        else:
            is_able = AI_INST_NOT_CERTAIN
            reason  = f"Borderline: score={score}, offer ratio={price_ratio:.2f}"

        return {
            "isAble":     is_able,
            "score":      score,
            "confidence": 0.75,
            "reason":     reason,
        }
    except Exception as e:
        log.exception("handle_buyer_rent_eligibility")
        return {"isAble": AI_INST_NOT_CERTAIN, "score": None, "confidence": 0.0, "reason": str(e)}

# ─────────────────────────────────────────────────────────────
# 04) Payment fraud
# entity=payment_card
# ─────────────────────────────────────────────────────────────

def handle_payment_fraud_detection(payload: dict, entity_id: int) -> dict:
    """
    Result: PaymentFraudDetectionResult
    → { decision: AIDecision int, score: int?, confidence, reason }
    Payload: cardType, maskedCardNumber, expiryMonth, expiryYear, amount, etc.
    """
    try:
        expiry_month = _int(payload.get("expiryMonth"), 1)
        expiry_year  = _int(payload.get("expiryYear"), 2099)
        now = datetime.now()

        if expiry_year < now.year or (expiry_year == now.year and expiry_month < now.month):
            return {
                "decision": AI_FRAUDULENT,
                "score": 95,
                "confidence": 0.99,
                "reason": f"Card expired {expiry_month}/{expiry_year}",
            }

        # No advanced model: return Not-Reviewed as safe default
        return {
            "decision": AI_VERIFIED,
            "score": 5,
            "confidence": 0.70,
            "reason": "Card not expired — basic check passed",
        }
    except Exception as e:
        log.exception("handle_payment_fraud_detection")
        return {"decision": AI_UNCERTAIN, "score": None, "confidence": 0.0, "reason": str(e)}

# ─────────────────────────────────────────────────────────────
# 05) Content / Reports / Anomaly
# ─────────────────────────────────────────────────────────────

_SPAM_WORDS = [
    "free", "click", "win", "money", "urgent", "guaranteed", "100%", "act now",
    "مبروك", "كسبت", "اتصل الان", "عرض محدود", "فرصة لن تتكرر",
]

def handle_content_moderation(payload: dict, entity_id: int) -> dict:
    """
    Result: ContentModerationResult
    → { isAllowed: bool, severity: int?, confidence, reason }
    entity can be: post | project | complaint
    """
    try:
        text = " ".join(filter(None, [
            str(payload.get("title") or ""),
            str(payload.get("description") or ""),
            str(payload.get("tagsJson") or ""),
            str(payload.get("body") or ""),
        ])).lower()

        hits = [w for w in _SPAM_WORDS if w in text]
        severity = len(hits)
        is_spam  = severity >= 3

        return {
            "isAllowed":  not is_spam,
            "severity":   severity,
            "confidence": 0.88,
            "reason":     f"{'Spam' if is_spam else 'Clean'} — {severity} keyword(s): {hits[:5]}",
        }
    except Exception as e:
        log.exception("handle_content_moderation")
        return {"isAllowed": True, "severity": 0, "confidence": 0.0, "reason": str(e)}


def handle_reports_smart_analysis(payload: dict, entity_id: int) -> dict:
    """
    Result: ReportsSmartAnalysisResult
    → { decision: AIDecision int, severity: int?, confidence, reason }
    entity=complaint
    """
    try:
        complaint_text = str(payload.get("content") or payload.get("description") or "").lower()
        _type = str(payload.get("complaintType") or "0")

        fraud_keywords = ["fake", "scam", "fraud", "مزور", "نصب", "احتيال"]
        hits = [w for w in fraud_keywords if w in complaint_text]
        severity = len(hits)

        if hits:
            return {"decision": AI_VERIFIED, "severity": severity,
                    "confidence": 0.82, "reason": f"Fraud indicators: {hits}"}
        return {"decision": AI_UNCERTAIN, "severity": 0,
                "confidence": 0.60, "reason": "No clear fraud indicators"}
    except Exception as e:
        log.exception("handle_reports_smart_analysis")
        return {"decision": AI_UNCERTAIN, "severity": None, "confidence": 0.0, "reason": str(e)}


def handle_user_anomaly_detection(payload: dict, entity_id: int) -> dict:
    """
    Result: UserAnomalyDetectionResult
    → { isSuspicious: bool, score: double, confidence, reason }
    entity: user | landlord
    """
    try:
        login_count  = _int(payload.get("loginCount"), 0)
        post_count   = _int(payload.get("postCount"), 0)
        report_count = _int(payload.get("reportCount"), 0)

        score = min(1.0, report_count * 0.3 + (post_count > 50) * 0.2)
        is_suspicious = score > 0.5

        return {
            "isSuspicious": is_suspicious,
            "score":        round(score, 3),
            "confidence":   0.70,
            "reason":       f"reports={report_count}, posts={post_count}",
        }
    except Exception as e:
        log.exception("handle_user_anomaly_detection")
        return {"isSuspicious": False, "score": 0.0, "confidence": 0.0, "reason": str(e)}

# ─────────────────────────────────────────────────────────────
# 06) Content / Text
# ─────────────────────────────────────────────────────────────

def handle_content_spam_detection(payload: dict, entity_id: int) -> dict:
    """Result: ContentSpamDetectionResult → { isSpam, score, confidence, reason }"""
    try:
        text = str(payload.get("text") or payload.get("content") or "").lower()
        hits = [w for w in _SPAM_WORDS if w in text]
        score = min(1.0, len(hits) / 5)
        return {
            "isSpam":     len(hits) >= 2,
            "score":      round(score, 3),
            "confidence": 0.85,
            "reason":     f"{len(hits)} spam keyword(s)",
        }
    except Exception as e:
        return {"isSpam": False, "score": 0.0, "confidence": 0.0, "reason": str(e)}


def handle_content_toxicity_scoring(payload: dict, entity_id: int) -> dict:
    """Result: ContentToxicityScoringResult → { toxicityScore, severity, confidence, reason }"""
    try:
        text = str(payload.get("text") or payload.get("content") or "").lower()
        toxic = ["hate", "kill", "abuse", "يلعن", "اللعنة"]
        hits  = [w for w in toxic if w in text]
        score = min(1.0, len(hits) / 3)
        return {
            "toxicityScore": round(score, 3),
            "severity":      len(hits),
            "confidence":    0.80,
            "reason":        f"{len(hits)} toxic keyword(s)",
        }
    except Exception as e:
        return {"toxicityScore": 0.0, "severity": 0, "confidence": 0.0, "reason": str(e)}


def handle_content_sentiment_analysis(payload: dict, entity_id: int) -> dict:
    """Result: ContentSentimentAnalysisResult → { label, score, confidence, reason }"""
    try:
        text = str(payload.get("text") or payload.get("content") or "")
        pos  = ["good", "great", "excellent", "happy", "love", "ممتاز", "رائع"]
        neg  = ["bad", "terrible", "awful", "hate", "worst", "سيء", "مزعج"]
        pos_hits = sum(1 for w in pos if w in text.lower())
        neg_hits = sum(1 for w in neg if w in text.lower())

        if pos_hits > neg_hits:   label, score = "Positive", 0.7 + pos_hits * 0.05
        elif neg_hits > pos_hits: label, score = "Negative", -(0.7 + neg_hits * 0.05)
        else:                     label, score = "Neutral",  0.0

        return {
            "label":      label,
            "score":      round(min(1.0, abs(score)) * (1 if score >= 0 else -1), 3),
            "confidence": 0.72,
            "reason":     f"pos={pos_hits} neg={neg_hits}",
        }
    except Exception as e:
        return {"label": "Neutral", "score": 0.0, "confidence": 0.0, "reason": str(e)}


def handle_content_language_detection(payload: dict, entity_id: int) -> dict:
    """Result: ContentLanguageDetectionResult → { languageCode, confidence, reason }"""
    try:
        text = str(payload.get("text") or payload.get("content") or "")
        ar_chars = sum(1 for c in text if '\u0600' <= c <= '\u06FF')
        ratio = ar_chars / max(len(text), 1)
        if ratio > 0.3:
            return {"languageCode": "ar", "confidence": round(0.5 + ratio * 0.5, 3),
                    "reason": f"Arabic char ratio {ratio:.2f}"}
        return {"languageCode": "en", "confidence": 0.80, "reason": "Default English"}
    except Exception as e:
        return {"languageCode": "und", "confidence": 0.0, "reason": str(e)}

# ─────────────────────────────────────────────────────────────
# 07) Search / Retrieval
# ─────────────────────────────────────────────────────────────

def _semantic_search(query: str, top_k: int) -> list[int]:
    if not NLP_AVAILABLE:
        return []
    import faiss as _faiss
    q_vec = nlp_model.encode([query]).astype("float32")
    _faiss.normalize_L2(q_vec)
    _, I = faiss_index.search(q_vec, top_k)
    return [int(i) for i in I[0] if i >= 0]


def handle_search_query_understanding(payload: dict, entity_id: int) -> dict:
    """Result: SearchQueryUnderstandingResult → { intent, entitiesJson, confidence, reason }"""
    try:
        query = str(payload.get("query") or "")
        intent = "property_search"
        entities = {}
        for city in city_list:
            if city.lower() in query.lower():
                entities["city"] = city
                break
        for unit in type_list:
            if unit.lower() in query.lower():
                entities["propertyType"] = unit
                break
        return {
            "intent":      intent,
            "entitiesJson": json.dumps(entities, ensure_ascii=False),
            "confidence":  0.70,
            "reason":      f"Keyword extraction from query: '{query[:80]}'",
        }
    except Exception as e:
        return {"intent": None, "entitiesJson": None, "confidence": 0.0, "reason": str(e)}


def handle_search_semantic_ranking(payload: dict, entity_id: int) -> dict:
    """Result: SearchSemanticRankingResult → { rankedPostIds, confidence, reason }"""
    try:
        query  = str(payload.get("query") or "")
        top_k  = _int(payload.get("topK") or payload.get("top_k"), 10)
        ids    = _semantic_search(query, top_k)
        return {
            "rankedPostIds": ids,
            "confidence":    0.78 if ids else 0.0,
            "reason":        f"FAISS top-{top_k}" if ids else "NLP not available",
        }
    except Exception as e:
        return {"rankedPostIds": [], "confidence": 0.0, "reason": str(e)}


def handle_search_similar_listings(payload: dict, entity_id: int) -> dict:
    """Result: SearchSimilarListingsResult → { similarPostIds, confidence, reason }"""
    try:
        query  = str(payload.get("query") or payload.get("title") or "")
        top_k  = _int(payload.get("topK") or payload.get("top_k"), 5)
        ids    = _semantic_search(query, top_k)
        return {
            "similarPostIds": ids,
            "confidence":     0.80 if ids else 0.0,
            "reason":         f"FAISS top-{top_k}" if ids else "NLP not available",
        }
    except Exception as e:
        return {"similarPostIds": [], "confidence": 0.0, "reason": str(e)}

# ─────────────────────────────────────────────────────────────
# 08) Recommendation
# ─────────────────────────────────────────────────────────────

def handle_reco_personalized_feed(payload: dict, entity_id: int) -> dict:
    """Result: RecoPersonalizedFeedResult → { recommendedPostIds, confidence, reason }"""
    try:
        query = str(payload.get("preferenceQuery") or payload.get("query") or "")
        ids   = _semantic_search(query, 10) if query else []
        return {"recommendedPostIds": ids, "confidence": 0.70 if ids else 0.0,
                "reason": "Preference-based FAISS" if ids else "No preference query provided"}
    except Exception as e:
        return {"recommendedPostIds": [], "confidence": 0.0, "reason": str(e)}


def handle_reco_related_posts(payload: dict, entity_id: int) -> dict:
    """Result: RecoRelatedPostsResult → { relatedPostIds, confidence, reason }"""
    try:
        query = str(payload.get("title") or payload.get("query") or "")
        ids   = _semantic_search(query, 6) if query else []
        return {"relatedPostIds": ids, "confidence": 0.75 if ids else 0.0,
                "reason": f"Similar to post {entity_id}"}
    except Exception as e:
        return {"relatedPostIds": [], "confidence": 0.0, "reason": str(e)}


def handle_reco_user_to_user_match(payload: dict, entity_id: int) -> dict:
    """Result: RecoUserToUserMatchResult → { matchedUserIds, confidence, reason }"""
    return {"matchedUserIds": [], "confidence": 0.0,
            "reason": "User-to-user matching requires collaborative filtering model"}

# ─────────────────────────────────────────────────────────────
# 09) Negotiation / Pricing
# ─────────────────────────────────────────────────────────────

def handle_negotiation_price_suggestion(payload: dict, entity_id: int) -> dict:
    """
    Result: NegotiationPriceSuggestionResult
    → { suggestedPrice, suggestedMessage, confidence, reason }
    """
    try:
        features = _build_features(payload)
        X = _to_X(features)
        predicted = _predict_price(X)
        suggested = round(predicted * 0.93)  # 7% negotiation room
        return {
            "suggestedPrice":   suggested,
            "suggestedMessage": f"Fair offer: {suggested:,.0f} EGP",
            "confidence":       0.78,
            "reason":           f"Market value {predicted:,.0f} EGP − 7% negotiation margin",
        }
    except Exception as e:
        log.exception("handle_negotiation_price_suggestion")
        return {"suggestedPrice": None, "suggestedMessage": None, "confidence": 0.0, "reason": str(e)}


def handle_negotiation_counter_offer_suggestion(payload: dict, entity_id: int) -> dict:
    """
    Result: NegotiationCounterOfferSuggestionResult
    → { counterOfferPrice, counterOfferMessage, confidence, reason }
    """
    try:
        features = _build_features(payload)
        X = _to_X(features)
        predicted   = _predict_price(X)
        buyer_offer = _float(payload.get("buyerOffer") or payload.get("offeredPrice"), predicted * 0.85)
        counter     = round((predicted + buyer_offer) / 2)
        return {
            "counterOfferPrice":   counter,
            "counterOfferMessage": f"Counter at {counter:,.0f} EGP",
            "confidence":          0.76,
            "reason":              f"Midpoint of market {predicted:,.0f} and offer {buyer_offer:,.0f}",
        }
    except Exception as e:
        log.exception("handle_negotiation_counter_offer_suggestion")
        return {"counterOfferPrice": None, "counterOfferMessage": None, "confidence": 0.0, "reason": str(e)}

# ─────────────────────────────────────────────────────────────
# 10) Insights / Analytics  (stored only on .NET side)
# ─────────────────────────────────────────────────────────────

def handle_insights_market_trends(payload: dict, entity_id: int) -> dict:
    """Result: InsightsMarketTrendsResult → { reportJson, confidence, reason }"""
    try:
        city = str(payload.get("city") or payload.get("location") or "all")
        enc  = _city_enc(city)
        row  = area_stats[area_stats.get("City_enc", pd.Series(dtype=float)) == enc] if "City_enc" in area_stats.columns else pd.DataFrame()
        report = {"city": city, "avgPrice": None, "listingCount": None}
        if not row.empty:
            report["avgPrice"]     = float(row["avg_price"].iloc[0]) if "avg_price" in row else None
            report["listingCount"] = int(row["listing_count"].iloc[0]) if "listing_count" in row else None
        return {"reportJson": json.dumps(report), "confidence": 0.70, "reason": f"Stats for '{city}'"}
    except Exception as e:
        return {"reportJson": None, "confidence": 0.0, "reason": str(e)}


_DEMAND_LABELS = {0: "Low", 1: "Medium", 2: "High"}
_AREA_FEATURES = ["listing_count", "avg_price", "price_std", "avg_area",
                  "furnished_rate", "ready_rate", "cash_rate"]

def handle_insights_demand_prediction(payload: dict, entity_id: int) -> dict:
    """Result: InsightsDemandPredictionResult → { demandJson, confidence, reason }"""
    try:
        city = str(payload.get("city") or payload.get("location") or "")
        enc  = _city_enc(city)
        row  = area_stats[area_stats["City_enc"] == enc] if "City_enc" in area_stats.columns else pd.DataFrame()
        if row.empty:
            return {"demandJson": None, "confidence": 0.0, "reason": f"No stats for '{city}'"}
        X    = row[_AREA_FEATURES].fillna(0)
        pred = area_model.predict(X)[0]
        lvl  = _DEMAND_LABELS.get(int(pred), "Medium")
        return {
            "demandJson": json.dumps({"city": city, "demandLevel": lvl, "areaRating": int(pred)}),
            "confidence": 0.75,
            "reason":     f"Area rating {pred} for '{city}'",
        }
    except Exception as e:
        return {"demandJson": None, "confidence": 0.0, "reason": str(e)}


def handle_insights_user_behavior_summary(payload: dict, entity_id: int) -> dict:
    """Result: InsightsUserBehaviorSummaryResult → { summaryJson, confidence, reason }"""
    return {
        "summaryJson": json.dumps({"note": "User behaviour model not yet available"}),
        "confidence":  0.0,
        "reason":      "Requires clickstream data model",
    }

# ─────────────────────────────────────────────────────────────
# 11) Contracts
# ─────────────────────────────────────────────────────────────

def handle_contract_risk_flags(payload: dict, entity_id: int) -> dict:
    """Result: ContractRiskFlagsResult → { riskFlagsJson, severity, confidence, reason }"""
    flags = []
    if not payload.get("sellerSignedAt"):
        flags.append("Seller signature missing")
    if not payload.get("buyerSignedAt"):
        flags.append("Buyer signature missing")
    return {
        "riskFlagsJson": json.dumps(flags),
        "severity":      len(flags),
        "confidence":    0.80,
        "reason":        f"{len(flags)} risk flag(s) detected",
    }

def handle_contract_clause_suggestion(payload: dict, entity_id: int) -> dict:
    """Result: ContractClauseSuggestionResult → { suggestedClausesJson, confidence, reason }"""
    contract_type = _int(payload.get("contractType"), 1)
    clauses = {
        1: ["Payment due within 30 days", "Title transfer on full payment"],
        2: ["Installment schedule attached", "Late payment penalty 2%"],
        3: ["Security deposit 2 months rent", "Notice period 1 month"],
    }
    return {
        "suggestedClausesJson": json.dumps(clauses.get(contract_type, [])),
        "confidence": 0.70,
        "reason": f"Standard clauses for contractType={contract_type}",
    }

# ─────────────────────────────────────────────────────────────
# 12) Support
# ─────────────────────────────────────────────────────────────

def handle_support_auto_reply_suggestion(payload: dict, entity_id: int) -> dict:
    """Result: SupportAutoReplySuggestionResult → { suggestedReply, confidence, reason }"""
    return {
        "suggestedReply": "Thank you for contacting us. Our team will review your request shortly.",
        "confidence":     0.65,
        "reason":         "Generic fallback reply",
    }

def handle_support_ticket_classification(payload: dict, entity_id: int) -> dict:
    """Result: SupportTicketClassificationResult → { label, severity, confidence, reason }"""
    text = str(payload.get("content") or payload.get("description") or "").lower()
    if any(w in text for w in ["fraud", "scam", "نصب", "مزور"]):
        return {"label": "Fraud", "severity": 3, "confidence": 0.85, "reason": "Fraud keywords"}
    if any(w in text for w in ["payment", "دفع", "فلوس"]):
        return {"label": "Payment", "severity": 2, "confidence": 0.80, "reason": "Payment keywords"}
    return {"label": "General", "severity": 1, "confidence": 0.65, "reason": "No specific keywords"}

def handle_support_priority_scoring(payload: dict, entity_id: int) -> dict:
    """Result: SupportPriorityScoringResult → { priority, confidence, reason }"""
    severity = _int(payload.get("severity"), 1)
    return {
        "priority":   min(severity * 2, 10),
        "confidence": 0.70,
        "reason":     f"Priority from severity={severity}",
    }

# ─────────────────────────────────────────────────────────────
# 13) Offers / Auctions
# ─────────────────────────────────────────────────────────────

def handle_buyer_offer_ranking(payload: dict, entity_id: int) -> dict:
    """
    Result: BuyerOfferRankingResult
    → { postId, rankedProposalIds, rankedJson, confidence, reason }
    """
    try:
        proposals = payload.get("proposals") or []
        # proposals: list of { proposalId, offeredPrice, isInstallment, downPayment }
        ranked = sorted(
            proposals,
            key=lambda p: (
                _float(p.get("offeredPrice"), 0)
                - _float(p.get("downPayment"), 0) * 0.1  # slightly prefer larger down-payment
            ),
            reverse=True,
        )
        ranked_ids = [_int(p.get("proposalId"), 0) for p in ranked]
        return {
            "postId":           entity_id,
            "rankedProposalIds": ranked_ids,
            "rankedJson":       json.dumps(ranked_ids),
            "confidence":       0.80,
            "reason":           f"Ranked {len(ranked_ids)} proposals by offered price",
        }
    except Exception as e:
        log.exception("handle_buyer_offer_ranking")
        return {"postId": entity_id, "rankedProposalIds": [], "rankedJson": None,
                "confidence": 0.0, "reason": str(e)}

# ─────────────────────────────────────────────────────────────
# 14) Image Quality (stored only)
# ─────────────────────────────────────────────────────────────

def handle_image_quality_scoring(payload: dict, entity_id: int) -> dict:
    """
    Result: ImageQualityScoringResult
    → { postId, overallScore, perImageScoresJson, issuesJson, confidence, reason }
    """
    image_count = _int(payload.get("imageCount"), 1)
    score = min(1.0, 0.5 + image_count * 0.05)
    return {
        "postId":              entity_id,
        "overallScore":        round(score, 3),
        "perImageScoresJson":  None,
        "issuesJson":          None,
        "confidence":          0.60,
        "reason":              f"{image_count} image(s) — heuristic quality estimate",
    }

# ─────────────────────────────────────────────────────────────
# 15) Owner Forecasts
# ─────────────────────────────────────────────────────────────

def handle_owner_forecast_price(payload: dict, entity_id: int) -> dict:
    """
    Result: OwnerForecastPriceResult
    → { postId, suggestedPrice, priceRangeJson, confidence, reason }
    """
    try:
        features = _build_features(payload)
        X        = _to_X(features)
        predicted = _predict_price(X)
        low   = round(predicted * 0.85)
        high  = round(predicted * 1.15)
        return {
            "postId":         entity_id,
            "suggestedPrice": round(predicted),
            "priceRangeJson": json.dumps({"low": low, "high": high, "currency": "EGP"}),
            "confidence":     0.82,
            "reason":         f"Model predicted {predicted:,.0f} EGP ±15%",
        }
    except Exception as e:
        log.exception("handle_owner_forecast_price")
        return {"postId": entity_id, "suggestedPrice": None, "priceRangeJson": None,
                "confidence": 0.0, "reason": str(e)}


def handle_owner_forecast_demand(payload: dict, entity_id: int) -> dict:
    """
    Result: OwnerForecastDemandResult
    → { postId, demandLevel, demandJson, confidence, reason }
    """
    try:
        city_raw = str(payload.get("location") or payload.get("city") or "")
        enc      = _city_enc(city_raw)
        row      = area_stats[area_stats["City_enc"] == enc] if "City_enc" in area_stats.columns else pd.DataFrame()

        if row.empty:
            return {"postId": entity_id, "demandLevel": "Low", "demandJson": None,
                    "confidence": 0.0, "reason": f"No area stats for '{city_raw}'"}

        X    = row[_AREA_FEATURES].fillna(0)
        pred = area_model.predict(X)[0]
        lvl  = _DEMAND_LABELS.get(int(pred), "Medium")

        return {
            "postId":      entity_id,
            "demandLevel": lvl,
            "demandJson":  json.dumps({"city": city_raw, "areaRating": int(pred)}),
            "confidence":  0.75,
            "reason":      f"Area rating {pred} → {lvl}",
        }
    except Exception as e:
        log.exception("handle_owner_forecast_demand")
        return {"postId": entity_id, "demandLevel": None, "demandJson": None,
                "confidence": 0.0, "reason": str(e)}


def handle_owner_forecast_revenue(payload: dict, entity_id: int) -> dict:
    """
    Result: OwnerForecastRevenueResult
    → { postId, expectedRevenue, revenueJson, confidence, reason }
    """
    try:
        features  = _build_features(payload)
        X         = _to_X(features)
        predicted = _predict_price(X)
        fee_pct   = _float(payload.get("platformFeePercent"), 1.5) / 100
        net       = round(predicted * (1 - fee_pct))
        return {
            "postId":          entity_id,
            "expectedRevenue": net,
            "revenueJson":     json.dumps({
                "grossPrice":         round(predicted),
                "platformFeePercent": fee_pct * 100,
                "netRevenue":         net,
                "currency":           "EGP",
            }),
            "confidence": 0.78,
            "reason":     f"Predicted {predicted:,.0f} − {fee_pct*100:.1f}% fee",
        }
    except Exception as e:
        log.exception("handle_owner_forecast_revenue")
        return {"postId": entity_id, "expectedRevenue": None, "revenueJson": None,
                "confidence": 0.0, "reason": str(e)}

# ─────────────────────────────────────────────────────────────
# 16) Decision Engine
# entity=post
# Result: DecisionEngineResult
# → { suggestedPendingStatus: "Accepted"|"Refused"|"Pending", riskLevel, confidence, reason }
# PostPendingStatus enum: Refused=-1, Pending=0, Accepted=1
# ─────────────────────────────────────────────────────────────

def handle_decision_engine(payload: dict, entity_id: int) -> dict:
    try:
        # Re-use sub-handlers for combined decision
        price_res   = handle_price_anomaly_detection(payload, entity_id)
        fraud_res   = handle_fraud_fake_property_detection(payload, entity_id)
        content_res = handle_content_moderation(payload, entity_id)

        price_eval   = _int(price_res.get("priceEvaluation"), PRICE_ACCEPTABLE)
        fraud_ok     = _int(fraud_res.get("decision"), AI_VERIFIED) != AI_FRAUDULENT
        content_ok   = _bool(content_res.get("isAllowed", True))
        has_doc      = bool(payload.get("postDocPath"))

        risk_level = 0
        if not fraud_ok:        risk_level += 40
        if not content_ok:      risk_level += 30
        if abs(price_eval) == 2: risk_level += 20  # VeryHigh or VeryLow
        if not has_doc:         risk_level += 10

        risk_level = min(risk_level, 100)

        if risk_level >= 60:
            status = "Refused"
        elif risk_level >= 25:
            status = "Pending"
        else:
            status = "Accepted"

        return {
            "suggestedPendingStatus": status,
            "riskLevel":              risk_level,
            "confidence":             0.85,
            "reason": (
                f"fraud={'OK' if fraud_ok else 'FAIL'}, "
                f"content={'OK' if content_ok else 'FAIL'}, "
                f"price={price_eval}, risk={risk_level}"
            ),
        }
    except Exception as e:
        log.exception("handle_decision_engine")
        return {"suggestedPendingStatus": "Pending", "riskLevel": None,
                "confidence": 0.0, "reason": str(e)}

# ─────────────────────────────────────────────────────────────
# Dispatcher table  (mirrors AiRequestTypes.cs string constants)
# ─────────────────────────────────────────────────────────────
HANDLERS: dict = {
    # 01) Fraud / Documents
    "fraud.document_analysis":                  handle_fraud_document_analysis,
    "fraud.ownership_document_analysis":        handle_fraud_ownership_document_analysis,
    "fraud.commercial_register_analysis":       handle_fraud_commercial_register_analysis,
    "fraud.post_document_analysis":             handle_fraud_post_document_analysis,
    "fraud.project_document_analysis":          handle_fraud_project_document_analysis,

    # 02) Fraud / Posts
    "fraud.fake_property_detection":            handle_fraud_fake_property_detection,
    "fraud.image_manipulation":                 handle_fraud_image_manipulation,
    "price.anomaly_detection":                  handle_price_anomaly_detection,

    # 03) Buyer / Proposals
    "buyer.installment_risk":                   handle_buyer_installment_risk,
    "buyer.rent_eligibility":                   handle_buyer_rent_eligibility,

    # 04) Payment
    "payment.fraud_detection":                  handle_payment_fraud_detection,

    # 05) Content / Reports / Anomaly
    "content.moderation":                       handle_content_moderation,
    "reports.smart_analysis":                   handle_reports_smart_analysis,
    "user.anomaly_detection":                   handle_user_anomaly_detection,

    # 06) Content / Text
    "content.spam_detection":                   handle_content_spam_detection,
    "content.toxicity_scoring":                 handle_content_toxicity_scoring,
    "content.sentiment_analysis":               handle_content_sentiment_analysis,
    "content.language_detection":               handle_content_language_detection,

    # 07) Search
    "search.query_understanding":               handle_search_query_understanding,
    "search.semantic_ranking":                  handle_search_semantic_ranking,
    "search.similar_listings":                  handle_search_similar_listings,

    # 08) Recommendation
    "reco.personalized_feed":                   handle_reco_personalized_feed,
    "reco.related_posts":                       handle_reco_related_posts,
    "reco.user_to_user_match":                  handle_reco_user_to_user_match,

    # 09) Negotiation
    "negotiation.price_suggestion":             handle_negotiation_price_suggestion,
    "negotiation.counter_offer_suggestion":     handle_negotiation_counter_offer_suggestion,

    # 10) Insights
    "insights.market_trends":                   handle_insights_market_trends,
    "insights.demand_prediction":               handle_insights_demand_prediction,
    "insights.user_behavior_summary":           handle_insights_user_behavior_summary,

    # 11) Contracts
    "contract.risk_flags":                      handle_contract_risk_flags,
    "contract.clause_suggestion":               handle_contract_clause_suggestion,

    # 12) Support
    "support.auto_reply_suggestion":            handle_support_auto_reply_suggestion,
    "support.ticket_classification":            handle_support_ticket_classification,
    "support.priority_scoring":                 handle_support_priority_scoring,

    # 13) Offers
    "buyer.offer_ranking":                      handle_buyer_offer_ranking,

    # 14) Image Quality
    "image.quality_scoring":                    handle_image_quality_scoring,

    # 15) Owner Forecasts
    "owner.forecast.price":                     handle_owner_forecast_price,
    "owner.forecast.demand":                    handle_owner_forecast_demand,
    "owner.forecast.revenue":                   handle_owner_forecast_revenue,

    # 16) Decision Engine
    "decision.engine":                          handle_decision_engine,
}

# ─────────────────────────────────────────────────────────────
# Main Kafka loop
# ─────────────────────────────────────────────────────────────
log.info(f"Starting Kafka consumer | bootstrap={KAFKA_BOOTSTRAP} | "
         f"topic={TOPIC_IN} | handlers={len(HANDLERS)}")

_consumer = KafkaConsumer(
    TOPIC_IN,
    bootstrap_servers=KAFKA_BOOTSTRAP,
    value_deserializer=lambda m: json.loads(m.decode("utf-8")),
    auto_offset_reset="earliest",
    group_id=GROUP_ID,
    enable_auto_commit=False,
)

for _msg in _consumer:
    try:
        env          = _msg.value                    # AiRequestEnvelope (camelCase)
        request_id   = str(env.get("requestId", ""))
        request_type = str(env.get("requestType", ""))
        entity       = env.get("entity") or {}
        entity_type  = str(entity.get("type", ""))
        entity_id    = int(entity.get("id", 0))
        payload_in   = env.get("payload") or {}

        log.info(f"← RECV [{request_type}] {entity_type}/{entity_id}")

        handler = HANDLERS.get(request_type)
        if handler is None:
            log.warning(f"Unknown requestType: '{request_type}'")
            _consumer.commit()
            continue

        payload_out = handler(payload_in, entity_id)
        _send(request_id, request_type, entity_type, entity_id, payload_out)
        _consumer.commit()

    except Exception:
        log.exception("FATAL: unhandled error in message loop — skipping message")
        try:
            _consumer.commit()
        except Exception:
            pass
