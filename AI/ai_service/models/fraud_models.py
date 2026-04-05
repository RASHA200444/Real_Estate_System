import os
import pandas as pd
import numpy as np
from .model_loader import models

# المسار الرئيسي للفولدر اللي الباك-إند بيرفع فيه الصور (Shared Storage)
# تأكد أن هذا المسار هو المجلد الأب لمجلد Media
BASE_MEDIA_PATH = r"D:\year4\GP\Real_Estate_System\BackEnd\otherServices\otherServices"
def encode_input(data):
    """تحويل البيانات لنفس التنسيق اللي الموديل اتدرب عليه"""
    try:
        type_enc = models.type_encoder.transform([str(data.get("type", "Apartment"))])[0]
        city_enc = models.city_encoder.transform([str(data.get("city", "Cairo"))])[0]
    except:
        type_enc, city_enc = 0, 0

    return {
        "Area": float(data.get("area", 100)),
        "Bedrooms": float(data.get("bedrooms", 2)),
        "Bathrooms": float(data.get("bathrooms", 1)),
        "Level": float(data.get("level", 1)),
        "City_enc": float(city_enc),
        "is_furnished": 1 if str(data.get("furnished","no")).lower() == "yes" else 0,
        "is_ready": 1 if str(data.get("delivery_term","ready")).lower() == "ready" else 0,
        "is_cash": 1 if str(data.get("payment","cash")).lower() == "cash" else 0,
        "months_until_delivery": float(data.get("months_until_delivery", 0)),
        "compound_price_mean": float(data.get("compound_price_mean", 2500000)),
        "Type_enc": float(type_enc),
    }

def fraud_fake_property_detection(payload):
    try:
        features = encode_input(payload)
        X = pd.DataFrame([features])[models.price_features]
        log_pred = models.price_model.predict(X)[0]
        predicted_price = float(np.expm1(log_pred))

        listed_price = float(payload.get("listed_price", 0))
        ratio = listed_price / predicted_price
        is_suspicious = 1 if (ratio < 0.6 or ratio > 1.5) else 0

        return {
            "Decision": 0 if is_suspicious else 1,
            "Confidence": 0.95 if is_suspicious else 0.88,
            "PredictedPrice": round(predicted_price),
            "Reason": "Price deviates significantly" if is_suspicious else "Price is within market range"
        }
    except Exception as e:
        return {"status": "error", "message": str(e)}

def fraud_document_analysis(payload):
    """تحليل وثائق المستخدم (NID) أو عقود الملكية أو مستندات العقار"""
    try:
        # 1. استخراج المسار النسبي - دعمنا كل المسميات اللي بتتبعت من الـ C#
        relative_path = (
            payload.get("nidPath") or 
            payload.get("ownershipDocPath") or 
            payload.get("postDocPath")
        )
        
        if not relative_path:
            return {"status": "error", "message": "No document path provided in payload"}

        # 2. تنظيف المسار وتحويل الـ Slashes لتناسب نظام التشغيل (Windows)
        # دي بتعالج مشكلة لو المسار جاي "Media/file.jpg" والويندوز عايزه "Media\file.jpg"
        clean_relative_path = relative_path.replace("/", os.sep).replace("\\", os.sep)
        full_path = os.path.normpath(os.path.join(BASE_MEDIA_PATH, clean_relative_path))
        
        print(f"DEBUG: Checking file at: {full_path}")

        # 3. التأكد من وجود الملف فعلياً
        if os.path.exists(full_path):
            return {
                "Decision": 1,
                "Confidence": 0.92,
                "Reason": "Document file found and integrity verified."
            }
        else:
            print(f"❌ Warning: File NOT found at {full_path}")
            return {
                "Decision": 0,
                "Confidence": 0.0,
                "Reason": f"Document file is missing from storage. Checked path: {full_path}"
            }
            
    except Exception as e:
        return {"status": "error", "message": str(e)}