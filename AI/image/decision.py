# image_dedup/decision.py
# Combines embedding similarity + perceptual hash for final duplicate decision

from config import Config
from similarity import hash_similarity, compute_phash
from PIL import Image
import imagehash

def make_decision(
    search_results: list,
    query_pil: Image.Image,
    stored_hashes: dict,        # image_id → imagehash object
) -> dict:
    """
    Two-stage duplicate detection:
    Stage 1: Embedding similarity > EMBEDDING_THRESHOLD
    Stage 2: pHash hamming distance < HASH_THRESHOLD

    Only declares duplicate if BOTH stages agree.
    This minimizes false positives aggressively.
    """

    query_hash = compute_phash(query_pil)

    for result in search_results:
        image_id       = result["image_id"]
        embed_score    = result["score"]

        # Stage 1: Check embedding similarity
        if embed_score < Config.EMBEDDING_THRESHOLD:
            continue   # not similar enough in embedding space

        # Stage 2: Check perceptual hash
        stored_hash = stored_hashes.get(image_id)
        if stored_hash is None:
            continue   # no hash stored, skip

        hash_sim, hash_dist = hash_similarity(query_hash, stored_hash)

        if hash_dist <= Config.HASH_THRESHOLD:
            # BOTH stages agree → confirmed duplicate
            return {
                "exists_before":      True,
                "matched_image_id":   image_id,
                "embedding_score":    round(embed_score, 4),
                "hash_similarity":    round(hash_sim, 4),
                "hash_distance":      hash_dist,
                "confidence":         "high",
                "decision_reason":    "Both embedding and perceptual hash match"
            }

    # No duplicate found
    return {
        "exists_before":    False,
        "matched_image_id": None,
        "embedding_score":  search_results[0]["score"] if search_results else 0.0,
        "hash_similarity":  0.0,
        "hash_distance":    999,
        "confidence":       "high",
        "decision_reason":  "No duplicate found above threshold"
    }