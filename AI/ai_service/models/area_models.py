# models/area_models.py
from .model_loader import models

def area_investment_rating(payload):
    try:
        city = str(payload.get("city", ""))
        try:
            city_enc = models.city_encoder.transform([city])[0]
        except:
            city_enc = 0

        # البحث عن بيانات المدينة في جدول الإحصائيات
        row = models.area_stats[models.area_stats["City_enc"] == city_enc]
        if row.empty:
            return {"status": "error", "message": "City statistics not found"}

        # الميزات اللي الموديل مستنيها
        AREA_FEATURES = ["listing_count", "avg_price", "price_std", 
                         "avg_area", "furnished_rate", "ready_rate", "cash_rate"]
        
        X = row[AREA_FEATURES].fillna(0)
        pred = models.area_model.predict(X)[0]
        
        labels = {0: "Red (High Risk)", 1: "Yellow (Moderate)", 2: "Green (High Potential)"}
        
        return {
            "city": city,
            "rating": labels[pred],
            "averagePrice": round(float(row["avg_price"].values[0])),
            "marketActivity": int(row["listing_count"].values[0]),
            "recommendation": "Strong demand area" if pred == 2 else "Stable market"
        }
    except Exception as e:
        return {"status": "error", "message": str(e)}