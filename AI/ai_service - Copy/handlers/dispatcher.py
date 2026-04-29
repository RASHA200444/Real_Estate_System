"""
handlers/dispatcher.py
=======================
Maps every AiRequestType string to the correct handler instance.
Adding a new handler = one line here.
"""

from __future__ import annotations
import logging
from typing import Any

from models.envelopes import AiRequestEnvelope
from models.request_types import AiRequestTypes
from handlers.base import BaseHandler

# ── import all handlers ──────────────────────────────────────
from handlers.fraud import (
    FraudDocumentAnalysisHandler,
    FraudOwnershipDocumentAnalysisHandler,
    FraudCommercialRegisterAnalysisHandler,
    FraudProjectDocumentAnalysisHandler,
    FraudFakePropertyDetectionHandler,
    FraudImageManipulationHandler,
    FraudPostDocumentAnalysisHandler,
)
from handlers.pricing import (
    PriceAnomalyDetectionHandler,
    NegotiationPriceSuggestionHandler,
    NegotiationCounterOfferSuggestionHandler,
)
from handlers.deal import (
    OwnerForecastPriceHandler,
    OwnerForecastDemandHandler,
    OwnerForecastRevenueHandler,
)
from handlers.search import (
    SearchSimilarListingsHandler,
    SearchSemanticRankingHandler,
    SearchQueryUnderstandingHandler,
    RecoRelatedPostsHandler,
    RecoPersonalizedFeedHandler,
    RecoUserToUserMatchHandler,
)
from handlers.content import (
    ContentModerationHandler,
    ContentSpamDetectionHandler,
    ContentToxicityScoringHandler,
    ContentSentimentAnalysisHandler,
    ContentLanguageDetectionHandler,
    ReportsSmartAnalysisHandler,
    UserAnomalyDetectionHandler,
)
from handlers.misc import (
    BuyerInstallmentRiskHandler,
    BuyerRentEligibilityHandler,
    BuyerOfferRankingHandler,
    PaymentFraudDetectionHandler,
    ImageQualityScoringHandler,
    DecisionEngineHandler,
    SupportAutoReplySuggestionHandler,
    SupportTicketClassificationHandler,
    SupportPriorityScoringHandler,
    InsightsMarketTrendsHandler,
    InsightsDemandPredictionHandler,
    InsightsUserBehaviorSummaryHandler,
    ContractRiskFlagsHandler,
    ContractClauseSuggestionHandler,
)

log = logging.getLogger(__name__)


class HandlerDispatcher:
    """
    Singleton-style dispatcher.
    Each handler is instantiated once and reused for every message.
    """

    def __init__(self) -> None:
        self._registry: dict[str, BaseHandler] = {
            # 01) Fraud / Documents
            AiRequestTypes.FRAUD_DOCUMENT_ANALYSIS:            FraudDocumentAnalysisHandler(),
            AiRequestTypes.FRAUD_OWNERSHIP_DOCUMENT_ANALYSIS:  FraudOwnershipDocumentAnalysisHandler(),
            AiRequestTypes.FRAUD_COMMERCIAL_REGISTER_ANALYSIS: FraudCommercialRegisterAnalysisHandler(),
            AiRequestTypes.FRAUD_PROJECT_DOCUMENT_ANALYSIS:    FraudProjectDocumentAnalysisHandler(),

            # 02) Fraud / Posts
            AiRequestTypes.FRAUD_FAKE_PROPERTY_DETECTION: FraudFakePropertyDetectionHandler(),
            AiRequestTypes.FRAUD_IMAGE_MANIPULATION:      FraudImageManipulationHandler(),
            AiRequestTypes.FRAUD_POST_DOCUMENT_ANALYSIS:  FraudPostDocumentAnalysisHandler(),
            AiRequestTypes.PRICE_ANOMALY_DETECTION:       PriceAnomalyDetectionHandler(),

            # 03) Buyer / Proposals
            AiRequestTypes.BUYER_INSTALLMENT_RISK: BuyerInstallmentRiskHandler(),
            AiRequestTypes.BUYER_RENT_ELIGIBILITY: BuyerRentEligibilityHandler(),

            # 04) Payment
            AiRequestTypes.PAYMENT_FRAUD_DETECTION: PaymentFraudDetectionHandler(),

            # 05) Content / Reports / Anomaly
            AiRequestTypes.CONTENT_MODERATION:    ContentModerationHandler(),
            AiRequestTypes.SMART_REPORTS_ANALYSIS: ReportsSmartAnalysisHandler(),
            AiRequestTypes.USER_ANOMALY_DETECTION: UserAnomalyDetectionHandler(),

            # 06) Content / Text
            AiRequestTypes.CONTENT_SPAM_DETECTION:     ContentSpamDetectionHandler(),
            AiRequestTypes.CONTENT_TOXICITY_SCORING:   ContentToxicityScoringHandler(),
            AiRequestTypes.CONTENT_SENTIMENT_ANALYSIS: ContentSentimentAnalysisHandler(),
            AiRequestTypes.CONTENT_LANGUAGE_DETECTION: ContentLanguageDetectionHandler(),

            # 07) Search
            AiRequestTypes.SEARCH_QUERY_UNDERSTANDING: SearchQueryUnderstandingHandler(),
            AiRequestTypes.SEARCH_SEMANTIC_RANKING:    SearchSemanticRankingHandler(),
            AiRequestTypes.SEARCH_SIMILAR_LISTINGS:    SearchSimilarListingsHandler(),

            # 08) Recommendation
            AiRequestTypes.RECO_PERSONALIZED_FEED:  RecoPersonalizedFeedHandler(),
            AiRequestTypes.RECO_RELATED_POSTS:      RecoRelatedPostsHandler(),
            AiRequestTypes.RECO_USER_TO_USER_MATCH: RecoUserToUserMatchHandler(),

            # 09) Negotiation
            AiRequestTypes.NEGOTIATION_PRICE_SUGGESTION:         NegotiationPriceSuggestionHandler(),
            AiRequestTypes.NEGOTIATION_COUNTER_OFFER_SUGGESTION: NegotiationCounterOfferSuggestionHandler(),

            # 10) Insights
            AiRequestTypes.INSIGHTS_MARKET_TRENDS:         InsightsMarketTrendsHandler(),
            AiRequestTypes.INSIGHTS_DEMAND_PREDICTION:     InsightsDemandPredictionHandler(),
            AiRequestTypes.INSIGHTS_USER_BEHAVIOR_SUMMARY: InsightsUserBehaviorSummaryHandler(),

            # 11) Contracts
            AiRequestTypes.CONTRACT_RISK_FLAGS:       ContractRiskFlagsHandler(),
            AiRequestTypes.CONTRACT_CLAUSE_SUGGESTION: ContractClauseSuggestionHandler(),

            # 12) Support
            AiRequestTypes.SUPPORT_AUTO_REPLY_SUGGESTION: SupportAutoReplySuggestionHandler(),
            AiRequestTypes.SUPPORT_TICKET_CLASSIFICATION: SupportTicketClassificationHandler(),
            AiRequestTypes.SUPPORT_PRIORITY_SCORING:      SupportPriorityScoringHandler(),

            # 13) Offers
            AiRequestTypes.BUYER_OFFER_RANKING: BuyerOfferRankingHandler(),

            # 14) Image quality
            AiRequestTypes.IMAGE_QUALITY_SCORING: ImageQualityScoringHandler(),

            # 15) Owner forecasts
            AiRequestTypes.OWNER_FORECAST_PRICE:   OwnerForecastPriceHandler(),
            AiRequestTypes.OWNER_FORECAST_DEMAND:  OwnerForecastDemandHandler(),
            AiRequestTypes.OWNER_FORECAST_REVENUE: OwnerForecastRevenueHandler(),

            # 16) Decision engine
            AiRequestTypes.DECISION_ENGINE: DecisionEngineHandler(),
        }

    def dispatch(self, envelope: AiRequestEnvelope) -> dict[str, Any]:
        handler = self._registry.get(envelope.requestType)
        if handler is None:
            log.warning("No handler registered for requestType='%s'", envelope.requestType)
            return {"error": f"Unknown requestType: {envelope.requestType}"}

        return handler.handle(
            payload=envelope.payload,
            entity_type=envelope.entity.type,
            entity_id=envelope.entity.id,
        )
