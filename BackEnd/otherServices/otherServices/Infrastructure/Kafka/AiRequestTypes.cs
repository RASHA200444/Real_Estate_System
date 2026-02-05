namespace otherServices.Infrastructure.Kafka;

/// <summary>
/// AI Request Types (modules)
/// - Backend بيرسل AiRequestEnvelope على ai.requests باستخدام واحد من القيم دي
/// - AI بيرجع AiResultEnvelope على ai.results بنفس الـ RequestType
/// - AiResultHandler بيعمل routing بناءً على RequestType
/// </summary>
public static class AiRequestTypes
{
    // =========================================================================
    // 01) Fraud / Documents (3)
    // =========================================================================
    public const string Fraud_DocumentAnalysis = "fraud.document_analysis"; // Users.NID
    public const string Fraud_OwnershipDocumentAnalysis = "fraud.ownership_document_analysis"; // Landlords.OwnershipDoc
    public const string Fraud_CommercialRegisterAnalysis = "fraud.commercial_register_analysis"; // Companies.CommercialRegister

    // =========================================================================
    // 02) Fraud / Posts (4)  ✅ (زودنا واحد جديد للـ PostDoc)
    // =========================================================================
    public const string Fraud_FakePropertyDetection = "fraud.fake_property_detection"; // Posts fake listing
    public const string Fraud_ImageManipulation = "fraud.image_manipulation";          // Posts images
    public const string Fraud_PostDocumentAnalysis = "fraud.post_document_analysis";  // ✅ Posts.PostDocPath
    public const string Price_AnomalyDetection = "price.anomaly_detection";            // Posts price

    // =========================================================================
    // 03) Buyer / Proposals (2)
    // =========================================================================
    public const string Buyer_InstallmentRisk = "buyer.installment_risk";
    public const string Buyer_RentEligibility = "buyer.rent_eligibility";

    // =========================================================================
    // 04) Payment (1)
    // =========================================================================
    public const string Payment_FraudDetection = "payment.fraud_detection";

    // =========================================================================
    // 05) Content / Reports / Anomaly (3)
    // =========================================================================
    public const string Content_Moderation = "content.moderation";
    public const string Smart_ReportsAnalysis = "reports.smart_analysis";
    public const string User_AnomalyDetection = "user.anomaly_detection";

    // =========================================================================
    // 06) Content / Text (4)
    // =========================================================================
    public const string Content_SpamDetection = "content.spam_detection";
    public const string Content_ToxicityScoring = "content.toxicity_scoring";
    public const string Content_SentimentAnalysis = "content.sentiment_analysis";
    public const string Content_LanguageDetection = "content.language_detection";

    // =========================================================================
    // 07) Search / Retrieval (3)
    // =========================================================================
    public const string Search_QueryUnderstanding = "search.query_understanding";
    public const string Search_SemanticRanking = "search.semantic_ranking";
    public const string Search_SimilarListings = "search.similar_listings";

    // =========================================================================
    // 08) Recommendation / Personalization (3)
    // =========================================================================
    public const string Reco_PersonalizedFeed = "reco.personalized_feed";
    public const string Reco_RelatedPosts = "reco.related_posts";
    public const string Reco_UserToUserMatch = "reco.user_to_user_match";

    // =========================================================================
    // 09) Negotiation / Pricing (2)
    // =========================================================================
    public const string Negotiation_PriceSuggestion = "negotiation.price_suggestion";
    public const string Negotiation_CounterOfferSuggestion = "negotiation.counter_offer_suggestion";

    // =========================================================================
    // 10) Insights / Analytics (3)
    // =========================================================================
    public const string Insights_MarketTrends = "insights.market_trends";
    public const string Insights_DemandPrediction = "insights.demand_prediction";
    public const string Insights_UserBehaviorSummary = "insights.user_behavior_summary";

    // =========================================================================
    // 11) Contracts / Legal Assist (2)
    // =========================================================================
    public const string Contract_RiskFlags = "contract.risk_flags";
    public const string Contract_ClauseSuggestion = "contract.clause_suggestion";

    // =========================================================================
    // 12) Support / Operations (3)
    // =========================================================================
    public const string Support_AutoReplySuggestion = "support.auto_reply_suggestion";
    public const string Support_TicketClassification = "support.ticket_classification";
    public const string Support_PriorityScoring = "support.priority_scoring";




    public const string Fraud_ProjectDocumentAnalysis = "fraud.project_document_analysis";

}
