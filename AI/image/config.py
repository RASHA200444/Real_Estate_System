# image_dedup/config.py
# Central configuration for the entire dedup system

class Config:
    # ── Model ────────────────────────────────────────────────
    EMBEDDING_MODEL    = "resnet50"       # resnet50 / efficientnet / clip
    EMBEDDING_DIM      = 2048             # ResNet50 output dim
    DEVICE             = "cpu"            # "cuda" if you have GPU

    # ── Similarity thresholds ────────────────────────────────
    EMBEDDING_THRESHOLD = 0.92            # cosine similarity (0-1)
    HASH_THRESHOLD      = 10             # pHash hamming distance (lower = more similar)
    # Both must trigger for a duplicate to be declared

    # ── FAISS index ──────────────────────────────────────────
    FAISS_INDEX_PATH = r"D:\year4\GP\Real_Estate_System\AI\image\faiss_index.bin"
    METADATA_PATH    = r"D:\year4\GP\Real_Estate_System\AI\image\metadata.json"

    # ── Image processing ─────────────────────────────────────
    IMAGE_SIZE         = (224, 224)
    TOP_K              = 5               # number of nearest neighbors to check

    # ── Kafka ────────────────────────────────────────────────
    KAFKA_BOOTSTRAP    = "localhost:9092"
    KAFKA_INPUT_TOPIC  = "new-listing-images"
    KAFKA_OUTPUT_TOPIC = "image-validation-results"

    # ── API ──────────────────────────────────────────────────
    API_HOST           = "0.0.0.0"
    API_PORT           = 8000

    # ── Database ─────────────────────────────────────────────
    # Set to None to use JSON file storage instead of PostgreSQL
    DATABASE_URL       = None
    # DATABASE_URL = "postgresql://user:password@localhost/realestate"