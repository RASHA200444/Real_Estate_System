# image_dedup/similarity.py
# FAISS-based similarity search + perceptual hashing

import faiss
import numpy as np
import imagehash
from PIL import Image
from config import Config

class SimilaritySearch:
    """
    Manages FAISS index for fast nearest-neighbor search.
    """

    def __init__(self):
        self.dimension = Config.EMBEDDING_DIM
        self.index     = faiss.IndexFlatIP(self.dimension)  # Inner product = cosine on normalized vecs
        self.id_map    = []   # maps FAISS position → image_id

    def add_embedding(self, embedding: np.ndarray, image_id: str):
        """Add a new embedding to the index."""
        vec = embedding.reshape(1, -1).astype("float32")
        self.index.add(vec)
        self.id_map.append(image_id)

    def search(self, embedding: np.ndarray, top_k: int = None):
        """
        Search for similar embeddings.
        Returns list of (image_id, similarity_score) tuples.
        """
        if self.index.ntotal == 0:
            return []

        top_k = top_k or Config.TOP_K
        top_k = min(top_k, self.index.ntotal)

        vec         = embedding.reshape(1, -1).astype("float32")
        scores, idxs = self.index.search(vec, top_k)

        results = []
        for score, idx in zip(scores[0], idxs[0]):
            if idx >= 0:
                results.append({
                    "image_id": self.id_map[idx],
                    "score":    float(score),
                    "faiss_idx": int(idx)
                })
        return results

    def save(self, path: str = None):
        """Save FAISS index to disk."""
        path = path or Config.FAISS_INDEX_PATH
        faiss.write_index(self.index, path)
        print(f"FAISS index saved: {self.index.ntotal} embeddings")

    def load(self, path: str = None):
        """Load FAISS index from disk."""
        import os
        path = path or Config.FAISS_INDEX_PATH
        if os.path.exists(path):
            self.index = faiss.read_index(path)
            print(f"FAISS index loaded: {self.index.ntotal} embeddings")
        else:
            print("No existing FAISS index found. Starting fresh.")

    def get_count(self):
        return self.index.ntotal


def compute_phash(pil_image: Image.Image) -> imagehash.ImageHash:
    """Compute perceptual hash of image."""
    return imagehash.phash(pil_image)

def hash_similarity(hash1: imagehash.ImageHash, hash2: imagehash.ImageHash) -> float:
    """
    Compute similarity from hamming distance.
    Returns 0-1 where 1 = identical.
    """
    distance   = hash1 - hash2       # hamming distance
    max_dist   = len(hash1.hash) ** 2
    similarity = 1 - (distance / max_dist)
    return similarity, distance