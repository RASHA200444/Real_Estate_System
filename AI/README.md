# Real Estate AI Project

## How to start everything

### Step 1 - Start Kafka (run once, keep window open)
```
C:\kafka\bin\windows\kafka-server-start.bat C:\kafka\config\server.properties
```

### Step 2 - Start AI Service (run in VS Code terminal)
```
python C:\Users\micheal\RealEstateAI\ai_service\ai_service.py
```

### Step 3 - Your .NET backend sends JSON to Kafka topics

---

## Kafka Topics

| Request Topic | Response Topic | Purpose |
|---|---|---|
| price-prediction-request | price-prediction-response | Predict property price |
| deal-classifier-request | deal-classifier-response | Is it a good deal? |
| area-rater-request | area-rater-response | Rate an area for investment |
| nlp-search-request | nlp-search-response | Natural language search |

---

## Input/Output JSON format

### Price Prediction
Input:
```json
{
  "area": 120,
  "bedrooms": 3,
  "bathrooms": 2,
  "level": 3,
  "city": "New Cairo - El Tagamoa",
  "type": "Apartment",
  "furnished": "yes",
  "delivery_term": "ready",
  "payment": "cash",
  "months_until_delivery": 0,
  "compound_price_mean": 2300000,
  "listed_price": 1800000
}
```
Output:
```json
{
  "status": "success",
  "predicted_price": 1121548,
  "range_low": 953316,
  "range_high": 1289780,
  "deal_label": "overpriced",
  "currency": "EGP"
}
```

### Deal Classifier
Input:
```json
{
  "area": 120,
  "bedrooms": 3,
  "bathrooms": 2,
  "level": 3,
  "city": "New Cairo - El Tagamoa",
  "type": "Apartment",
  "furnished": "yes",
  "delivery_term": "ready",
  "payment": "cash",
  "months_until_delivery": 0,
  "compound_price_mean": 2300000
}
```
Output:
```json
{
  "status": "success",
  "deal_label": "fair_price",
  "confidence": 72.3,
  "breakdown": {
    "great_deal": 15.2,
    "fair_price": 72.3,
    "overpriced": 12.5
  }
}
```

### Area Rater
Input:
```json
{
  "city": "New Cairo - El Tagamoa"
}
```
Output:
```json
{
  "status": "success",
  "city": "New Cairo - El Tagamoa",
  "rating": "green",
  "description": "High potential - strong demand area",
  "avg_price": 3500000,
  "listing_count": 12037
}
```

### NLP Search
Input:
```json
{
  "query": "3 bedroom apartment in New Cairo furnished ready",
  "top_k": 5
}
```
Output:
```json
{
  "status": "success",
  "query": "3 bedroom apartment in New Cairo furnished ready",
  "results": [
    {
      "rank": 1,
      "listing_idx": 1234,
      "description": "Apartment 3 bedrooms New Cairo 120 sqm furnished ready",
      "score": 0.923
    }
  ]
}
```

---

## Saved Models

| File | Purpose |
|---|---|
| price_predictor.pkl | Predict property price |
| deal_classifier.pkl | Classify deal quality |
| price_range_classifier.pkl | Predict price range |
| area_rater.pkl | Rate area investment potential |
| area_stats_table.pkl | Area statistics lookup |
| seasonal_demand_models.pkl | Seasonal demand forecast |
| listing_search.faiss | NLP search index |
| Type_encoder.pkl | Encode property type |
| City_encoder.pkl | Encode city name |
| Price_Range_encoder.pkl | Encode price range |