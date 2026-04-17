# main.py
from ai_service.kafka_service.consumer import AiKafkaConsumer
from ai_service.kafka_service.producer import AiKafkaProducer
from ai_service.dispatcher.ai_dispatcher import AiDispatcher
from ai_service.config import REQUEST_TOPIC, RESULT_TOPIC

consumer = AiKafkaConsumer(REQUEST_TOPIC)
producer = AiKafkaProducer()
dispatcher = AiDispatcher()

def start_ai_service():
    print("AI Service started...")

    for message in consumer.listen():
        try:
            # --- [PRINT 1] تأكيد استلام الرسالة وشكلها ---
            print(f"\n[DEBUG] New message received: {message}") 
            
            requestId = message.get("requestId")
            requestType = message.get("requestType")
            payload = message.get("payload", {})

            if not requestId or not requestType:
                # --- [PRINT 2] التحقق من البيانات الأساسية ---
                print(f"[SKIP] Missing requestId or requestType in message: {message}")
                continue

            print(f"Processing {requestType} : {requestId}")

            # --- [PRINT 3] قبل الدخول في متاهة الـ AI ---
            print(f"[DEBUG] Calling dispatcher for {requestType}...") 
            
            # هنا غالباً المنطقة اللي الكود بيقف فيها
            result_payload = dispatcher.dispatch(requestType, payload)

            # --- [PRINT 4] بعد خروج النتيجة من الـ AI ---
            print(f"[DEBUG] AI Analysis completed successfully. Result: {result_payload}") 

            result_message = {
            "requestId": requestId,
            "requestType": requestType,
            "entity": message.get("entity"), # مهم جداً ترجع الـ entity عشان الـ Handler يعرف يوصل لـ ID اليوزر/الـ Landlord
            "payload": result_payload # غيرنا 'result' لـ 'payload'
        }

            # --- [PRINT 5] قبل الإرسال لكافكا ---
            print(f"[DEBUG] Sending result to Kafka topic: {RESULT_TOPIC}") 
            
            producer.send(RESULT_TOPIC, result_message)
            
            # --- [PRINT 6] تأكيد نجاح الدورة كاملة ---
            print(f"[SUCCESS] Message {requestId} sent to {RESULT_TOPIC}") 

        except Exception as e:
            # --- [PRINT 7] أهم برينت: كشف العطل الحقيقي ---
            print(f"\n❌❌ [FATAL ERROR] Failed to process message {message.get('requestId', 'unknown')}")
            print(f"Details: {str(e)}")
            # لو عايز تفاصيل أكتر عن السطر اللي ضرب
            import traceback
            traceback.print_exc() 

if __name__ == "__main__":
    start_ai_service()