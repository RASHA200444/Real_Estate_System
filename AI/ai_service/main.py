# main.py
import traceback
from datetime import datetime, timezone
from ai_service.kafka_service.consumer import AiKafkaConsumer
from ai_service.kafka_service.producer import AiKafkaProducer
from ai_service.dispatcher.ai_dispatcher import AiDispatcher
from ai_service.config import REQUEST_TOPIC, RESULT_TOPIC

# تهيئة الخدمات
consumer = AiKafkaConsumer(REQUEST_TOPIC)
producer = AiKafkaProducer()
dispatcher = AiDispatcher()

def start_ai_service():
    print("🚀 AI Service started and listening for requests...")

    for message in consumer.listen():
        try:
            # --- [PRINT 1] تأكيد استلام الرسالة وشكلها ---
            print(f"\n[DEBUG] New message received: {message}") 
            
            # استخراج البيانات الأساسية (دوت نت يستخدم CamelCase أو PascalCase والكونسومر يتعامل معها)
            requestId = message.get("requestId") or message.get("RequestId")
            requestType = message.get("requestType") or message.get("RequestType")
            payload = message.get("payload") or message.get("Payload", {})
            entity = message.get("entity") or message.get("Entity")

            if not requestId or not requestType:
                # --- [PRINT 2] التحقق من البيانات الأساسية ---
                print(f"[SKIP] Missing requestId or requestType in message: {message}")
                continue

            print(f"🛠️ Processing {requestType} | ID: {requestId}")

            # --- [PRINT 3] قبل الدخول في منطق الـ AI ---
            print(f"[DEBUG] Calling dispatcher for {requestType}...") 
            
            # تمرير البايلود للموديل المناسب
            result_payload = dispatcher.dispatch(requestType, payload)

            # --- [PRINT 4] بعد خروج النتيجة من الـ AI ---
            print(f"[DEBUG] AI Analysis completed. Result: {result_payload}") 

            # بناء الرسالة النهائية لتطابق كلاس AiResultEnvelope.cs في الدوت نت
            result_message = {
                "RequestId": requestId,
                "RequestType": requestType,
                "Entity": entity, # نرجعه كما هو (Type & Id) لضمان التتبع في الباك
                "Payload": result_payload,
                "ProcessedAtUtc": datetime.now(timezone.utc).isoformat() # توقيت المعالجة بصيغة ISO
            }

            # --- [PRINT 5] قبل الإرسال لكافكا ---
            print(f"[DEBUG] Sending result to Kafka topic: {RESULT_TOPIC}") 
            
            producer.send(RESULT_TOPIC, result_message)
            
            # --- [PRINT 6] تأكيد نجاح الدورة كاملة ---
            print(f"✅ [SUCCESS] Message {requestId} sent back to {RESULT_TOPIC}") 

        except Exception as e:
            # --- [PRINT 7] كشف العطل الحقيقي ---
            print(f"\n❌❌ [FATAL ERROR] Failed to process message {message.get('requestId', 'unknown')}")
            print(f"Details: {str(e)}")
            traceback.print_exc() 

if __name__ == "__main__":
    start_ai_service()