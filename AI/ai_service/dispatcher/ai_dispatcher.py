from ai_service.models.fraud_models import fraud_fake_property_detection
from ai_service.models.fraud_models import fraud_document_analysis
from ai_service.models.buyer_models import buyer_installment_risk
from ai_service.models.content_models import content_spam_detection
from ai_service.models.area_models import area_investment_rating # جديد
from ai_service.models.search_models import semantic_search_engine # جديد

class AiDispatcher:

    def dispatch(self, requestType, payload):
        print(f"DEBUG: Dispatching requestType: '{requestType}'")

        # 01) Fraud / Documents
        if requestType == "fraud.document_analysis":
            return fraud_document_analysis(payload)
            
        # (لو عندك موديول للـ ownership ممكن تضيفه هنا)
        if requestType == "fraud.ownership_document_analysis":
            # لو الموديول لسه مش جاهز ممكن ترجع رد مؤقت
            print("Warning: Ownership analysis requested but not fully implemented.")
            return fraud_document_analysis(payload) 

        # 02) Fraud / Posts
        if requestType == "fraud.fake_property_detection":
            return fraud_fake_property_detection(payload)

        # 03) Buyer / Proposals
        if requestType == "buyer.installment_risk":
            return buyer_installment_risk(payload)

        # 04) Area Analysis
        if requestType == "area.investment_rating":
            return area_investment_rating(payload)

        # 05) Semantic Search
        if requestType == "search.semantic_query":
            return semantic_search_engine(payload)

        # 06) Content / Text
        if requestType == "content.spam_detection":
            return content_spam_detection(payload)

        # 01) Projects
        if requestType == "fraud.project_document_analysis":
             return fraud_document_analysis(payload)

        # لو جالك طلب مش موجود في الـ AI service حالياً
        print(f"⚠️ Warning: Request type '{requestType}' received but no handler defined.")
        return {"status": "error", "message": f"Handler for {requestType} not implemented yet"}