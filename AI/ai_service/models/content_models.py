def content_spam_detection(payload):
    # هنا ممكن نستخدم الـ NLP model اللي عندك عشان نحسب "التشابه" مع جمل سبام معروفة
    text = payload.get("text", "")
    
    # حالياً هنخليها Logic محسّن بالـ NLP لو حبيت، 
    # بس لو عايز تحافظ على الـ Spam words التقليدية:
    spam_words = ["free", "click", "offer", "win", "money", "urgent"]
    score = sum(1 for word in spam_words if word in text.lower())
    
    return {
        "IsSpam": score > 2,
        "Score": score,
        "Confidence": 0.89,
        "Reason": f"Detected {score} suspicious keywords"
    }