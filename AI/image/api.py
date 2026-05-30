# image_dedup/api.py
# FastAPI REST endpoint for direct image checking

import uuid
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from config import Config
from embedding import get_model
from similarity import SimilaritySearch
from decision import make_decision
from storage import Storage
from ingestion import process_image_url

app     = FastAPI(title="Real Estate Image Dedup API")
model   = get_model()
storage = Storage()
searcher = SimilaritySearch()
searcher.load()
searcher.id_map = list(storage.metadata.keys())

class ImageRequest(BaseModel):
    image_url:  str
    listing_id: str = "unknown"
    image_id:   str = None
    save_if_new: bool = True    # save to index if not duplicate

class ImageResponse(BaseModel):
    image_id:         str
    listing_id:       str
    exists_before:    bool
    matched_image_id: str = None
    embedding_score:  float
    hash_similarity:  float
    hash_distance:    int
    confidence:       str
    decision_reason:  str
    total_indexed:    int

@app.post("/check-image")
async def check_image(request: ImageRequest):
    try:
        image_id = request.image_id or str(uuid.uuid4())

        # Process image
        pil, tensor = process_image_url(request.image_url)
        embedding   = model.get_embedding(tensor)

        # Search
        results  = searcher.search(embedding)

        # Decide
        decision = make_decision(
            search_results = results,
            query_pil      = pil,
            stored_hashes  = storage.get_all_hashes()
        )

        # Save if new
        if not decision["exists_before"] and request.save_if_new:
            storage.save_image(
                image_id          = image_id,
                embedding         = embedding,
                pil_image         = pil,
                listing_id        = request.listing_id,
                similarity_search = searcher
            )

        return {
            "image_id":         image_id,
            "listing_id":       request.listing_id,
            "total_indexed":    storage.get_total_count(),
            "exists_before":    decision["exists_before"],
            "matched_image_id": decision["matched_image_id"],
            "embedding_score":  decision["embedding_score"],
            "hash_similarity":  decision["hash_similarity"],
            "hash_distance":    decision["hash_distance"],
            "confidence":       decision["confidence"],
            "decision_reason":  decision["decision_reason"]
        }

    except Exception as e:
        print(f"API Error: {e}")
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/stats")
def get_stats():
    """Get system statistics."""
    return {
        "total_images_indexed": storage.get_total_count(),
        "faiss_index_size":     searcher.get_count(),
        "embedding_threshold":  Config.EMBEDDING_THRESHOLD,
        "hash_threshold":       Config.HASH_THRESHOLD,
        "model":                Config.EMBEDDING_MODEL,
    }

@app.get("/health")
def health():
    return {"status": "ok"}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host=Config.API_HOST, port=Config.API_PORT)