# models/search_models.py
import faiss
from .model_loader import models

def semantic_search_engine(payload):
    try:
        query = str(payload.get("query", ""))
        top_k = int(payload.get("limit", 5)) # الباك-إند ممكن يحدد عدد النتائج

        # تحويل النص لـ Vector
        q_vec = models.nlp_model.encode([query]).astype("float32")
        faiss.normalize_L2(q_vec)
        
        # البحث في قاعدة بيانات الفيكتورز
        distances, indices = models.faiss_index.search(q_vec, top_k)

        results = []
        for rank, (idx, score) in enumerate(zip(indices[0], distances[0])):
            results.append({
                "rank": rank + 1,
                "listingId": int(idx), # الـ Index بتاع العقار
                "matchScore": round(float(score), 3),
                "previewText": models.listing_texts.iloc[idx]["listing_text"][:150] + "..."
            })

        return {
            "query": query,
            "totalFound": len(results),
            "results": results
        }
    except Exception as e:
        return {"status": "error", "message": str(e)}