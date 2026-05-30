# image_dedup/embedding.py
# Extracts dense vector embeddings from images using ResNet50

import torch
import torch.nn as nn
import numpy as np
from torchvision import models
from config import Config

class EmbeddingModel:
    """
    Wraps a pre-trained ResNet50 model.
    Removes the final classification layer to get embeddings.
    """

    def __init__(self):
        print("Loading embedding model...")
        self.device = torch.device(Config.DEVICE)

        # Load pretrained ResNet50
        base_model = models.resnet50(pretrained=True)

        # Remove final FC layer — we want the 2048-dim feature vector
        self.model = nn.Sequential(*list(base_model.children())[:-1])
        self.model.eval()
        self.model.to(self.device)
        print(f"Embedding model loaded on {Config.DEVICE}")

    def get_embedding(self, tensor) -> np.ndarray:
        """
        Takes a preprocessed tensor, returns normalized embedding vector.
        Shape: (2048,)
        """
        with torch.no_grad():
            tensor  = tensor.to(self.device)
            output  = self.model(tensor)
            embedding = output.squeeze().cpu().numpy()

        # L2 normalize for cosine similarity via dot product
        norm = np.linalg.norm(embedding)
        if norm > 0:
            embedding = embedding / norm

        return embedding.astype("float32")

    def get_embedding_from_url(self, image_url: str) -> tuple:
        """
        Full pipeline: URL → embedding
        Returns: (embedding, pil_image)
        """
        from ingestion import process_image_url
        pil, tensor = process_image_url(image_url)
        embedding   = self.get_embedding(tensor)
        return embedding, pil

# Singleton — load once, reuse
_model_instance = None

def get_model() -> EmbeddingModel:
    global _model_instance
    if _model_instance is None:
        _model_instance = EmbeddingModel()
    return _model_instance