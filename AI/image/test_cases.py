# image_dedup/test_cases.py
# Test all functions end to end

import requests
import json
import time
from kafka import KafkaProducer, KafkaConsumer
from config import Config

API_BASE = f"http://localhost:{Config.API_PORT}"

# ── Test images (public real estate images) ──────────────────
TEST_IMAGES = [
    {
        "name":       "Apartment A - First upload",
        "image_url":  "https://images.pexels.com/photos/1571460/pexels-photo-1571460.jpeg",
        "listing_id": "listing_001",
        "expect_dup": False
    },
    {
        "name":       "Apartment A - Same image again",
        "image_url":  "https://images.pexels.com/photos/1571460/pexels-photo-1571460.jpeg",
        "listing_id": "listing_002",
        "expect_dup": True
    },
    {
        "name":       "Apartment B - Different image",
        "image_url":  "https://images.pexels.com/photos/1643383/pexels-photo-1643383.jpeg",
        "listing_id": "listing_003",
        "expect_dup": False
    },
]

def test_api():
    """Test via REST API."""
    print("\n" + "="*60)
    print("TESTING VIA REST API")
    print("="*60)

    passed = 0
    failed = 0

    for test in TEST_IMAGES:
        print(f"\nTest: {test['name']}")
        print(f"URL:  {test['image_url']}")

        try:
            response = requests.post(
                f"{API_BASE}/check-image",
                json={
                    "image_url":  test["image_url"],
                    "listing_id": test["listing_id"]
                },
                timeout=30
            )
            result = response.json()

            print(f"Result: exists_before={result['exists_before']}")
            print(f"        embedding_score={result['embedding_score']}")
            print(f"        hash_distance={result['hash_distance']}")
            print(f"        reason={result['decision_reason']}")
            print(f"        total_indexed={result['total_indexed']}")

            if result["exists_before"] == test["expect_dup"]:
                print(f"PASS")
                passed += 1
            else:
                print(f"FAIL - Expected exists_before={test['expect_dup']}")
                failed += 1

        except Exception as e:
            print(f"ERROR: {e}")
            failed += 1

        time.sleep(1)

    print(f"\n{'='*60}")
    print(f"Results: {passed} passed, {failed} failed")
    print(f"{'='*60}")

def test_kafka():
    """Test via Kafka topics."""
    print("\n" + "="*60)
    print("TESTING VIA KAFKA")
    print("="*60)

    producer = KafkaProducer(
        bootstrap_servers=Config.KAFKA_BOOTSTRAP,
        value_serializer=lambda v: json.dumps(v).encode("utf-8")
    )

    consumer = KafkaConsumer(
        Config.KAFKA_OUTPUT_TOPIC,
        bootstrap_servers=Config.KAFKA_BOOTSTRAP,
        value_deserializer=lambda m: json.loads(m.decode("utf-8")),
        auto_offset_reset="latest",
        group_id=None,
        consumer_timeout_ms=15000
    )

    # Send test image
    producer.send(Config.KAFKA_INPUT_TOPIC, {
        "image_url":  TEST_IMAGES[0]["image_url"],
        "listing_id": "kafka_test_001",
        "image_id":   "kafka_img_001"
    })
    producer.flush()
    print("Sent image to Kafka. Waiting for result...")

    for message in consumer:
        result = message.value
        print(f"\nKafka result received:")
        print(json.dumps(result, indent=2))
        break

def test_stats():
    """Check system stats."""
    print("\n" + "="*60)
    print("SYSTEM STATS")
    print("="*60)
    response = requests.get(f"{API_BASE}/stats")
    print(json.dumps(response.json(), indent=2))

if __name__ == "__main__":
    print("Starting tests...")
    print("Make sure api.py is running first!\n")

    test_stats()
    test_api()
    test_kafka()