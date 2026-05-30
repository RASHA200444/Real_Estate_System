# image_dedup/ingestion.py
# Fetches image from URL, normalizes it, returns PIL image + numpy array

import requests
import numpy as np
from PIL import Image, ImageOps
from io import BytesIO
from config import Config

def fetch_image(image_url: str) -> Image.Image:
    """
    Fetch image from URL.
    Returns PIL Image in RGB format.
    """
    try:
        headers = {"User-Agent": "RealEstateAI/1.0"}
        response = requests.get(image_url, timeout=10, headers=headers)
        response.raise_for_status()
        image = Image.open(BytesIO(response.content))
        return image
    except Exception as e:
        raise ValueError(f"Failed to fetch image from {image_url}: {e}")

def normalize_image(image: Image.Image) -> Image.Image:
    """
    Normalize image:
    - Convert to RGB (remove alpha, handle grayscale)
    - Resize to standard size
    - Remove EXIF metadata
    """
    # Convert to RGB
    image = image.convert("RGB")

    # Remove EXIF metadata by re-saving through BytesIO
    clean = Image.new("RGB", image.size)
    clean.paste(image)

    # Resize to standard size
    clean = clean.resize(Config.IMAGE_SIZE, Image.LANCZOS)

    return clean

def image_to_tensor(image: Image.Image):
    """
    Convert PIL image to normalized PyTorch tensor.
    """
    import torch
    from torchvision import transforms

    transform = transforms.Compose([
        transforms.ToTensor(),
        transforms.Normalize(
            mean=[0.485, 0.456, 0.406],   # ImageNet mean
            std=[0.229, 0.224, 0.225]     # ImageNet std
        )
    ])
    tensor = transform(image).unsqueeze(0)  # add batch dim
    return tensor

def process_image_url(image_url: str):
    """
    Full pipeline: URL → normalized PIL image + tensor
    Returns: (pil_image, tensor)
    """
    raw    = fetch_image(image_url)
    pil    = normalize_image(raw)
    tensor = image_to_tensor(pil)
    return pil, tensor