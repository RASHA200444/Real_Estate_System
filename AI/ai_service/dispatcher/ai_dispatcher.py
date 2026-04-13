# ai_dispatcher.py
from ai_service.models.fraud_models import fraud_fake_property_detection, fraud_document_analysis
from ai_service.models.buyer_models import buyer_installment_risk
from ai_service.models.content_models import content_spam_detection
from ai_service.models.area_models import area_investment_rating
from ai_service.models.search_models import semantic_search_engine

class AiDispatcher:
    """
    الهدف: توجيه الطلب للموديل المناسب وضمان أن النتيجة (Decision) 
    تطابق قيم الـ Enums في الـ .NET Backend.
    """

    def dispatch(self, requestType, payload):
        print(f"🔍 [DISPATCHER] Handling: '{requestType}'")

        try:
            # --- 1. تحليل المستندات (AIDecision Enum) ---
            # Verified = 1, Fraudulent = 2, Uncertain = 0
            if requestType in ["fraud.document_analysis", 
                               "fraud.ownership_document_analysis", 
                               "fraud.project_document_analysis"]:
                
                result = fraud_document_analysis(payload)
                # التأكد من صحة المفاتيح (Payload في الـ C# يتوقع هؤلاء)
                return {
                    "Decision": result.get("Decision", 0), 
                    "Confidence": result.get("Confidence", 0.0),
                    "Reason": result.get("Reason", "Analysis completed")
                }

            # --- 2. كشف العقارات الوهمية (PostPendingStatus Enum) ---
            # Accepted = 1, Refused = -1, Pending = 0
            elif requestType == "fraud.fake_property_detection":
                result = fraud_fake_property_detection(payload)
                return {
                    "Decision": result.get("Decision", 0),
                    "Confidence": result.get("Confidence", 0.0),
                    "Reason": result.get("Reason", "")
                }

            # --- 3. تقييم مخاطر الأقساط (AIInstallmentDecision Enum) ---
            # Able = 1, Disable = -1, NotCertain = 0
            elif requestType == "buyer.installment_risk":
                result = buyer_installment_risk(payload)
                return {
                    "Decision": result.get("Decision", 0),
                    "Score": result.get("Score", 0), # متاح لو الباك محتاج الـ Risk Score
                    "Reason": result.get("Reason", "")
                }

            # --- 4. تقييم الاستثمار في المناطق (PriceEvaluation Enum) ---
            # Acceptable = 0, High = 1, VeryHigh = 2, Low = -1
            elif requestType == "area.investment_rating":
                return area_investment_rating(payload)

            # --- 5. محرك البحث الدلالي ---
            elif requestType == "search.semantic_query":
                return semantic_search_engine(payload)

            # --- 6. كشف الرسائل المزعجة (Spam) ---
            elif requestType == "content.spam_detection":
                return content_spam_detection(payload)

            # في حال وصول نوع طلب غير مدرج
            print(f"⚠️ Warning: No specific handler for '{requestType}'")
            return {
                "Status": "Error",
                "Message": f"Handler for {requestType} not defined."
            }

        except Exception as e:
            print(f"❌ Error in Dispatcher during {requestType}: {str(e)}")
            return {
                "Decision": 0, # Uncertain
                "Reason": f"AI Internal Error: {str(e)}"
            }