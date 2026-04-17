# models/model_loader.py
import joblib
import faiss
import pandas as pd
from sentence_transformers import SentenceTransformer
import os

# المسارات الجديدة حسب الـ dir اللي بعته
BASE_PATH = r"D:\year4\GP\Real_Estate_System\AI\models"
SAVE_PATH = os.path.join(BASE_PATH, "saved")
ENC_PATH = os.path.join(BASE_PATH, "encoders")

class ModelContainer:
    _instance = None

    def __init__(self):
        print("Loading Pretrained AI Models...")
        self.price_model = joblib.load(f"{SAVE_PATH}/price_predictor.pkl")
        self.price_features = joblib.load(f"{SAVE_PATH}/price_predictor_features.pkl")
        self.deal_model = joblib.load(f"{SAVE_PATH}/deal_classifier.pkl")
        self.type_encoder = joblib.load(f"{ENC_PATH}/Type_encoder.pkl")
        self.city_encoder = joblib.load(f"{ENC_PATH}/City_encoder.pkl")
        
        self.area_model = joblib.load(f"{SAVE_PATH}/area_rater.pkl")
        self.area_stats = joblib.load(f"{SAVE_PATH}/area_stats_table.pkl")
        
        # للبحث الذكي (NLP)
        self.faiss_index = faiss.read_index(f"{SAVE_PATH}/listing_search.faiss")
        self.listing_texts = pd.read_csv(f"{SAVE_PATH}/listing_texts.csv")
        self.nlp_model = SentenceTransformer("all-MiniLM-L6-v2")
        print("All Models (including Area & NLP) Loaded Successfully!")

    @classmethod
    def get_instance(cls):
        if cls._instance is None:
            cls._instance = ModelContainer()
        return cls._instance

# هنعمل instance واحد عشان م نستهلكش رامات كتير
models = ModelContainer.get_instance()