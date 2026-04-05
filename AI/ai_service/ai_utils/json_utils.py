import json


def serialize(obj):
    return json.dumps(obj).encode("utf-8")


def deserialize(data):
    return json.loads(data.decode("utf-8"))