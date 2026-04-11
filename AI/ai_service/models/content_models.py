# models/content_models.py
from .model_loader import models

def content_spam_detection(payload):
    text = payload.get("text", "")
    if not text:
        return {"isSpam": False, "score": 0}

    # المنطق التقليدي (سريع وفعال للكلمات المحظورة)
    spam_words = ["free", "click", "win", "money", "urgent", "مبروك", "كسبت"]
    matches = sum(1 for word in spam_words if word in text.lower())
    
    # ممكن مستقبلاً تقارن النص بجمل سبام محفوظة عندك باستخدام models.nlp_model
    
    return {
        "isSpam": matches > 2,
        "score": matches,
        "confidence": 0.90,
        "reason": f"Detected {matches} suspicious keywords"
    }