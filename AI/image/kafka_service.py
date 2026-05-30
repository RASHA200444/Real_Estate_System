# image_dedup/kafka_service.py
# Kafka consumer/producer for async image processing

import json
import uuid
from kafka import KafkaConsumer, KafkaProducer
from config import Config
from embedding import get_model
from similarity import SimilaritySearch
from decision import make_decision
from storage import Storage
from ingestion import process_image_url

def run_kafka_service():
    """
    Listens to 'new-listing-images' topic.
    Processes each image and sends result to 'image-validation-results'.

    Input JSON:
    {
        "image_url": "https://...",
        "listing_id": "listing_123",
        "image_id": "optional_custom_id"
    }

    Output JSON:
    {
        "image_id": "...",
        "listing_id": "...",
        "exists_before": true/false,
        "matched_image_id": "...",
        "embedding_score": 0.95,
        "hash_similarity": 0.97,
        "decision_reason": "..."
    }
    """

    print("Loading models and storage...")
    model    = get_model()
    storage  = Storage()
    searcher = SimilaritySearch()
    searcher.load()

    # Sync id_map with stored metadata
    searcher.id_map = list(storage.metadata.keys())

    producer = KafkaProducer(
        bootstrap_servers=Config.KAFKA_BOOTSTRAP,
        value_serializer=lambda v: json.dumps(v).encode("utf-8")
    )

    consumer = KafkaConsumer(
        Config.KAFKA_INPUT_TOPIC,
        bootstrap_servers=Config.KAFKA_BOOTSTRAP,
        value_deserializer=lambda m: json.loads(m.decode("utf-8")),
        auto_offset_reset="latest",
        group_id=None
    )

    print(f"Listening on topic: {Config.KAFKA_INPUT_TOPIC}")
    print("Waiting for image events...\n")

    for message in consumer:
        data = message.value
        print(f"Received: {data}")

        try:
            image_url  = data["image_url"]
            listing_id = data.get("listing_id", "unknown")
            image_id   = data.get("image_id", str(uuid.uuid4()))

            # Process image
            pil, tensor    = process_image_url(image_url)
            embedding, _   = model.get_embedding(tensor), pil
            embedding      = model.get_embedding(tensor)

            # Search for similar
            results = searcher.search(embedding)

            # Make decision
            decision = make_decision(
                search_results = results,
                query_pil      = pil,
                stored_hashes  = storage.get_all_hashes()
            )

            # If not duplicate → save to index
            if not decision["exists_before"]:
                storage.save_image(
                    image_id          = image_id,
                    embedding         = embedding,
                    pil_image         = pil,
                    listing_id        = listing_id,
                    similarity_search = searcher
                )

            # Send result back to Kafka
            result = {
                "image_id":         image_id,
                "listing_id":       listing_id,
                "image_url":        image_url,
                **decision
            }

            producer.send(Config.KAFKA_OUTPUT_TOPIC, result)
            producer.flush()
            print(f"Result sent: {result}\n")

        except Exception as e:
            error_result = {
                "image_id":     data.get("image_id", "unknown"),
                "listing_id":   data.get("listing_id", "unknown"),
                "status":       "error",
                "message":      str(e)
            }
            producer.send(Config.KAFKA_OUTPUT_TOPIC, error_result)
            producer.flush()
            print(f"Error: {e}")

if __name__ == "__main__":
    run_kafka_service()