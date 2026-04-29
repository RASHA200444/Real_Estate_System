"""
models/request_types.py
=======================
Python mirror of the C# AiRequestTypes constants.
Keeping the same string values ensures zero mismatch.
"""


class AiRequestTypes:
    # 01) Fraud / Documents
    FRAUD_DOCUMENT_ANALYSIS            = "fraud.document_analysis"
    FRAUD_OWNERSHIP_DOCUMENT_ANALYSIS  = "fraud.ownership_document_analysis"
    FRAUD_COMMERCIAL_REGISTER_ANALYSIS = "fraud.commercial_register_analysis"
    FRAUD_PROJECT_DOCUMENT_ANALYSIS    = "fraud.project_document_analysis"

    # 02) Fraud / Posts
    FRAUD_FAKE_PROPERTY_DETECTION = "fraud.fake_property_detection"
    FRAUD_IMAGE_MANIPULATION      = "fraud.image_manipulation"
    FRAUD_POST_DOCUMENT_ANALYSIS  = "fraud.post_document_analysis"
    PRICE_ANOMALY_DETECTION       = "price.anomaly_detection"

    # 03) Buyer / Proposals
    BUYER_INSTALLMENT_RISK  = "buyer.installment_risk"
    BUYER_RENT_ELIGIBILITY  = "buyer.rent_eligibility"

    # 04) Payment
    PAYMENT_FRAUD_DETECTION = "payment.fraud_detection"

    # 05) Content / Reports / Anomaly
    CONTENT_MODERATION    = "content.moderation"
    SMART_REPORTS_ANALYSIS = "reports.smart_analysis"
    USER_ANOMALY_DETECTION = "user.anomaly_detection"

    # 06) Content / Text
    CONTENT_SPAM_DETECTION    = "content.spam_detection"
    CONTENT_TOXICITY_SCORING  = "content.toxicity_scoring"
    CONTENT_SENTIMENT_ANALYSIS = "content.sentiment_analysis"
    CONTENT_LANGUAGE_DETECTION = "content.language_detection"

    # 07) Search / Retrieval
    SEARCH_QUERY_UNDERSTANDING = "search.query_understanding"
    SEARCH_SEMANTIC_RANKING    = "search.semantic_ranking"
    SEARCH_SIMILAR_LISTINGS    = "search.similar_listings"

    # 08) Recommendation
    RECO_PERSONALIZED_FEED  = "reco.personalized_feed"
    RECO_RELATED_POSTS      = "reco.related_posts"
    RECO_USER_TO_USER_MATCH = "reco.user_to_user_match"

    # 09) Negotiation / Pricing
    NEGOTIATION_PRICE_SUGGESTION         = "negotiation.price_suggestion"
    NEGOTIATION_COUNTER_OFFER_SUGGESTION = "negotiation.counter_offer_suggestion"

    # 10) Insights / Analytics
    INSIGHTS_MARKET_TRENDS         = "insights.market_trends"
    INSIGHTS_DEMAND_PREDICTION     = "insights.demand_prediction"
    INSIGHTS_USER_BEHAVIOR_SUMMARY = "insights.user_behavior_summary"

    # 11) Contracts / Legal
    CONTRACT_RISK_FLAGS       = "contract.risk_flags"
    CONTRACT_CLAUSE_SUGGESTION = "contract.clause_suggestion"

    # 12) Support / Operations
    SUPPORT_AUTO_REPLY_SUGGESTION  = "support.auto_reply_suggestion"
    SUPPORT_TICKET_CLASSIFICATION  = "support.ticket_classification"
    SUPPORT_PRIORITY_SCORING       = "support.priority_scoring"

    # 13) Offers / Auctions
    BUYER_OFFER_RANKING = "buyer.offer_ranking"

    # 14) Image Quality
    IMAGE_QUALITY_SCORING = "image.quality_scoring"

    # 15) Owner Forecasts
    OWNER_FORECAST_PRICE   = "owner.forecast.price"
    OWNER_FORECAST_DEMAND  = "owner.forecast.demand"
    OWNER_FORECAST_REVENUE = "owner.forecast.revenue"

    # 16) Decision Engine
    DECISION_ENGINE = "decision.engine"
