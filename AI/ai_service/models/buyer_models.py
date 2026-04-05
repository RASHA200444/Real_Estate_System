def buyer_installment_risk(payload):
    try:
        features = encode_input(payload)
        X = pd.DataFrame([features])[models.price_features]
        
        # استخدام موديل تصنيف الصفقات
        pred = models.deal_model.predict(X)[0]
        proba = models.deal_model.predict_proba(X)[0]
        
        # الموديل القديم كان فيه: [great_deal, fair_price, overpriced]
        # إحنا هنستخدم الـ Confidence بتاع الـ overpriced كمؤشر للمخاطرة
        risk_score = float(proba[2]) # احتمالية إنها overpriced
        
        return {
            "IsAble": 1 if risk_score < 0.7 else 0,
            "Score": round((1 - risk_score) * 100),
            "Confidence": round(float(max(proba)), 2),
            "Reason": "Safe investment based on deal classification" if risk_score < 0.5 else "High risk - property might be overpriced"
        }
    except Exception as e:
        return {"status": "error", "message": str(e)}