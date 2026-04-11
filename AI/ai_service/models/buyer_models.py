# models/buyer_models.py
import pandas as pd
from .model_loader import models
from .fraud_models import encode_input # استيراد دالة التشفير من ملف الفراود

def buyer_installment_risk(payload):
    try:
        features = encode_input(payload)
        X = pd.DataFrame([features])[models.price_features]
        
        proba = models.deal_model.predict_proba(X)[0]
        # ترتيب الكلاسات في الموديل: [great_deal(0), fair_price(1), overpriced(2)]
        risk_score = float(proba[2]) 
        
        return {
            "isAble": 1 if risk_score < 0.7 else 0,
            "score": round((1 - risk_score) * 100),
            "confidence": round(float(max(proba)), 2),
            "reason": "Safe investment" if risk_score < 0.5 else "High risk - overpriced"
        }
    except Exception as e:
        return {"status": "error", "message": str(e)}