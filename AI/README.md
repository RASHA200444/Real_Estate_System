# Real Estate AI Service

## كيفية التشغيل

### الخطوة 1 — تشغيل Kafka (مرة واحدة، اتركه شغال)
```
C:\kafka\bin\windows\zookeeper-server-start.bat C:\kafka\config\zookeeper.properties
C:\kafka\bin\windows\kafka-server-start.bat C:\kafka\config\server.properties
```

### الخطوة 2 — تشغيل الـ AI Service
```
cd D:\year4\GP\Real_Estate_System\AI
venv\Scripts\python.exe ai_service.py
```

---

## بنية الـ Kafka

الخدمة تعمل على **topic واحد** لكل الطلبات والردود:

| Topic | الاتجاه | الوصف |
|---|---|---|
| `ai.requests` | .NET → Python | كل الطلبات اللي بتجي من الـ backend |
| `ai.results` | Python → .NET | كل الردود اللي بترجع للـ backend |

---

## شكل الـ Envelope

### الطلب (من .NET)
```json
{
  "requestId": "71fb0f1d-be3d-4d19-aa90-2bb81d9fabc4",
  "requestType": "fraud.fake_property_detection",
  "entity": { "type": "post", "id": 10022 },
  "createdAtUtc": "2026-05-13T00:33:07.48Z",
  "payload": { }
}
```

### الرد (من Python)
```json
{
  "requestId": "71fb0f1d-be3d-4d19-aa90-2bb81d9fabc4",
  "requestType": "fraud.fake_property_detection",
  "entity": { "type": "post", "id": 10022 },
  "payload": { },
  "processedAtUtc": "2026-05-13T00:33:09.68Z"
}
```

---

## أنواع الطلبات (requestType) والـ Payload

### Fraud / Documents

| requestType | entity | payload fields | result fields |
|---|---|---|---|
| `fraud.document_analysis` | user | `nidPath` | `decision`, `confidence`, `reason` |
| `fraud.ownership_document_analysis` | landlord | `ownershipDocPath` | `decision`, `confidence`, `reason` |
| `fraud.commercial_register_analysis` | company | `commercialRegisterPath` | `decision`, `confidence`, `reason` |
| `fraud.post_document_analysis` | post | `postDocPath` | `decision`, `confidence`, `reason` |
| `fraud.project_document_analysis` | project | `projectDocPath` | `decision`, `confidence`, `reason` |

`decision` values → `AIDecision` enum: `0=Uncertain`, `1=Verified`, `2=Fraudulent`, `3=NotReviewed`

---

### Fraud / Posts

**`fraud.fake_property_detection`** — entity: post
```json
// payload
{ "price": 800000, "area": 90, "numberOfRooms": 3, "numberOfBathrooms": 2,
  "floorNumber": 6, "location": "Nasr City", "isFurnished": true }

// result
{ "decision": 1, "confidence": 0.88, "reason": "Price ratio 0.95 within normal range" }
```

**`fraud.image_manipulation`** — entity: post
```json
// result
{ "decision": 0, "confidence": 0.0, "reason": "Image dedup complete" }
```

**`price.anomaly_detection`** — entity: post
```json
// payload
{ "price": 800000, "area": 90, "numberOfRooms": 3, "location": "Nasr City" }

// result  (PriceEvaluation: VeryLow=-2, Low=-1, Acceptable=0, High=1, VeryHigh=2)
{ "priceEvaluation": -1, "confidence": 0.82, "reason": "Listed 800K vs predicted 1.1M (ratio=0.72)" }
```

---

### Buyer / Proposals

**`buyer.installment_risk`** — entity: proposal
```json
// payload
{ "offeredPrice": 1200000, "installmentDurationMonths": 60,
  "downPayment": 300000, "postPrice": 1500000 }

// result  (AIInstallmentDecision: Disable=-1, NotCertain=0, Able=1)
{ "isAble": 1, "score": 72, "confidence": 0.78, "reason": "Down payment 20%, score=72" }
```

**`buyer.rent_eligibility`** — entity: proposal
```json
// payload
{ "offeredPrice": 5000, "postPrice": 5500 }

// result  (AIRentDecision: Disable=-1, NotCertain=0, Able=1)
{ "isAble": 1, "score": 65, "confidence": 0.75, "reason": "Score=65, offer ratio=0.91" }
```

---

### Payment

**`payment.fraud_detection`** — entity: payment_card
```json
// payload
{ "cardType": 0, "maskedCardNumber": "****1234", "expiryMonth": 12, "expiryYear": 2027 }

// result
{ "decision": 1, "score": 5, "confidence": 0.70, "reason": "Card not expired — basic check passed" }
```

---

### Content

**`content.moderation`** — entity: post / project / complaint
```json
// payload
{ "title": "villa", "description": "large apartment", "tagsJson": "[\"garden\"]" }

// result
{ "isAllowed": true, "severity": 0, "confidence": 0.88, "reason": "Clean — 0 keyword(s)" }
```

**`content.spam_detection`** — entity: any
```json
// result
{ "isSpam": false, "score": 0.1, "confidence": 0.85, "reason": "1 spam keyword(s)" }
```

**`content.toxicity_scoring`** — entity: any
```json
// result
{ "toxicityScore": 0.0, "severity": 0, "confidence": 0.80, "reason": "0 toxic keyword(s)" }
```

**`content.sentiment_analysis`** — entity: any
```json
// result
{ "label": "Positive", "score": 0.75, "confidence": 0.72, "reason": "pos=2 neg=0" }
```

**`content.language_detection`** — entity: any
```json
// result
{ "languageCode": "ar", "confidence": 0.85, "reason": "Arabic char ratio 0.72" }
```

---

### Reports & Anomaly

**`reports.smart_analysis`** — entity: complaint
```json
// payload
{ "content": "this listing is fake", "complaintType": 2 }

// result
{ "decision": 1, "severity": 2, "confidence": 0.82, "reason": "Fraud indicators: ['fake']" }
```

**`user.anomaly_detection`** — entity: user / landlord
```json
// payload
{ "loginCount": 200, "postCount": 80, "reportCount": 5 }

// result
{ "isSuspicious": true, "score": 0.65, "confidence": 0.70, "reason": "reports=5, posts=80" }
```

---

### Search

**`search.query_understanding`** — entity: any
```json
// payload
{ "query": "3 bedroom apartment in Nasr City furnished" }

// result
{ "intent": "property_search", "entitiesJson": "{\"city\":\"Nasr City\"}", "confidence": 0.70, "reason": "..." }
```

**`search.semantic_ranking`** / **`search.similar_listings`** — entity: any
```json
// payload
{ "query": "...", "topK": 10 }

// result
{ "rankedPostIds": [101, 55, 234], "confidence": 0.78, "reason": "FAISS top-10" }
```

---

### Recommendation

| requestType | result fields |
|---|---|
| `reco.personalized_feed` | `recommendedPostIds`, `confidence`, `reason` |
| `reco.related_posts` | `relatedPostIds`, `confidence`, `reason` |
| `reco.user_to_user_match` | `matchedUserIds`, `confidence`, `reason` |

---

### Negotiation

**`negotiation.price_suggestion`** — entity: post
```json
// result
{ "suggestedPrice": 1023000, "suggestedMessage": "Fair offer: 1,023,000 EGP", "confidence": 0.78, "reason": "..." }
```

**`negotiation.counter_offer_suggestion`** — entity: proposal
```json
// payload
{ "buyerOffer": 900000, "area": 90, "location": "Nasr City" }

// result
{ "counterOfferPrice": 1050000, "counterOfferMessage": "Counter at 1,050,000 EGP", "confidence": 0.76, "reason": "..." }
```

---

### Insights

**`insights.market_trends`** — entity: any
```json
// payload  { "city": "Nasr City" }
// result   { "reportJson": "{\"city\":\"Nasr City\",\"avgPrice\":4000000,\"listingCount\":3200}", "confidence": 0.70, "reason": "..." }
```

**`insights.demand_prediction`** — entity: any
```json
// result
{ "demandJson": "{\"city\":\"Nasr City\",\"demandLevel\":\"High\",\"areaRating\":2}", "confidence": 0.75, "reason": "..." }
```

---

### Contracts

| requestType | entity | result fields |
|---|---|---|
| `contract.risk_flags` | contract | `riskFlagsJson`, `severity`, `confidence`, `reason` |
| `contract.clause_suggestion` | contract | `suggestedClausesJson`, `confidence`, `reason` |

---

### Support

| requestType | result fields |
|---|---|
| `support.auto_reply_suggestion` | `suggestedReply`, `confidence`, `reason` |
| `support.ticket_classification` | `label`, `severity`, `confidence`, `reason` |
| `support.priority_scoring` | `priority`, `confidence`, `reason` |

---

### Offers / Images / Forecasts

**`buyer.offer_ranking`** — entity: post
```json
// payload
{ "proposals": [ {"proposalId": 1, "offeredPrice": 1200000, "downPayment": 300000} ] }

// result
{ "postId": 10022, "rankedProposalIds": [1, 3, 2], "rankedJson": "[1,3,2]", "confidence": 0.80, "reason": "..." }
```

**`image.quality_scoring`** — entity: post
```json
// payload  { "imageCount": 5 }
// result   { "postId": 10022, "overallScore": 0.75, "perImageScoresJson": null, "issuesJson": null, "confidence": 0.60, "reason": "..." }
```

**`owner.forecast.price`** — entity: post
```json
// result
{ "postId": 10022, "suggestedPrice": 1100000, "priceRangeJson": "{\"low\":935000,\"high\":1265000,\"currency\":\"EGP\"}", "confidence": 0.82, "reason": "..." }
```

**`owner.forecast.demand`** — entity: post
```json
// result
{ "postId": 10022, "demandLevel": "High", "demandJson": "{\"city\":\"Nasr City\",\"areaRating\":2}", "confidence": 0.75, "reason": "..." }
```

**`owner.forecast.revenue`** — entity: post
```json
// result
{ "postId": 10022, "expectedRevenue": 1083500, "revenueJson": "{\"grossPrice\":1100000,\"platformFeePercent\":1.5,\"netRevenue\":1083500,\"currency\":\"EGP\"}", "confidence": 0.78, "reason": "..." }
```

---

### Decision Engine

**`decision.engine`** — entity: post — يجمع fraud + content + price في قرار واحد
```json
// payload
{ "title": "...", "description": "...", "location": "Nasr City",
  "price": 800000, "area": 90, "numberOfRooms": 3, "postDocPath": "Media/file.pdf" }

// result  (suggestedPendingStatus: "Accepted" | "Pending" | "Refused")
{ "suggestedPendingStatus": "Accepted", "riskLevel": 10, "confidence": 0.85,
  "reason": "fraud=OK, content=OK, price=0, risk=10" }
```

---

## الـ Models المستخدمة

| الملف | المجلد | الوصف |
|---|---|---|
| `price_predictor.pkl` | saved | XGBRegressor — يتنبأ بسعر العقار |
| `price_predictor_features.pkl` | saved | ترتيب الـ features المطلوبة للموديل |
| `deal_classifier.pkl` | saved | XGBClassifier — جودة الصفقة |
| `price_range_classifier.pkl` | saved | XGBClassifier — High/Low/Luxury/Middle |
| `area_rater.pkl` | saved | DecisionTreeClassifier — تقييم المنطقة 0/1/2 |
| `area_stats_table.pkl` | saved | DataFrame — إحصائيات المناطق |
| `seasonal_demand_models.pkl` | saved | (فارغ حالياً) |
| `City_encoder.pkl` | encoders | LabelEncoder للمدينة |
| `Type_encoder.pkl` | encoders | LabelEncoder لنوع الوحدة (ثابت: Apartment) |
| `Price_Range_encoder.pkl` | encoders | LabelEncoder لفئة السعر |

## Features الموديل

```
['Area', 'Bedrooms', 'Bathrooms', 'Level', 'City_enc',
 'is_furnished', 'is_ready', 'is_cash', 'months_until_delivery',
 'compound_price_mean', 'Type_enc']
```

## Mapping من .NET إلى الموديل

| .NET field (camelCase) | model feature | notes |
|---|---|---|
| `area` | `Area` | |
| `numberOfRooms` | `Bedrooms` | |
| `numberOfBathrooms` | `Bathrooms` | |
| `floorNumber` | `Level` | |
| `location` | `City_enc` | LabelEncoder lookup |
| `isFurnished` | `is_furnished` | bool → 0/1 |
| `monthsUntilDelivery` | `months_until_delivery` + `is_ready` | 0 → is_ready=1 |
| `isInstallment` | `is_cash` | Cash=0→1, Installment=1→0 |
| — | `Type_enc` | ثابت = Apartment |
| — | `compound_price_mean` | fallback من city lookup table |
