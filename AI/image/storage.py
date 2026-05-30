# image_dedup/storage.py
# Handles persistent storage of embeddings, hashes, and metadata

import json
import os
import numpy as np
import imagehash
from datetime import datetime
from config import Config

class Storage:
    """
    JSON-based persistent storage.
    Stores: image_id, listing_id, timestamp, phash, embedding path.
    Switch to PostgreSQL by replacing load/save methods.
    """

    def __init__(self):
        self.metadata    = {}    # image_id → {listing_id, timestamp, phash_str}
        self.hashes      = {}    # image_id → imagehash object
        self.load()

    def save_image(
        self,
        image_id:   str,
        embedding:  np.ndarray,
        pil_image,
        listing_id: str,
        similarity_search       # SimilaritySearch instance
    ):
        """Save new image embedding, hash, and metadata."""

        # Compute and store perceptual hash
        phash = imagehash.phash(pil_image)
        self.hashes[image_id]   = phash

        # Add to FAISS index
        similarity_search.add_embedding(embedding, image_id)

        # Store metadata
        self.metadata[image_id] = {
            "listing_id":  listing_id,
            "timestamp":   datetime.now().isoformat(),
            "phash":       str(phash),
            "image_id":    image_id
        }

        # Persist to disk
        self.save()
        similarity_search.save()

        print(f"Saved image {image_id} | Total: {len(self.metadata)}")

    def save(self):
        """Save metadata to JSON."""
        with open(Config.METADATA_PATH, "w") as f:
            json.dump(self.metadata, f, indent=2)

    def load(self):
        """Load metadata from JSON."""
        if os.path.exists(Config.METADATA_PATH):
            with open(Config.METADATA_PATH, "r") as f:
                self.metadata = json.load(f)

            # Reconstruct hash objects
            for image_id, meta in self.metadata.items():
                try:
                    self.hashes[image_id] = imagehash.hex_to_hash(meta["phash"])
                except:
                    pass

            print(f"Loaded {len(self.metadata)} image records from storage")
        else:
            print("No existing storage found. Starting fresh.")

    def get_all_hashes(self) -> dict:
        return self.hashes

    def get_metadata(self, image_id: str) -> dict:
        return self.metadata.get(image_id, {})

    def get_total_count(self) -> int:
        return len(self.metadata)