from flask import Flask, request, jsonify
from deepface import DeepFace
import cv2, numpy as np, base64

app = Flask(__name__)

@app.route("/analyze", methods=["POST"])
def analyze():
    try:
        data = request.get_json(force=True, silent=True) or {}
        if "image" not in data:
            return jsonify({"error": "no image"}), 400

        img_bytes = base64.b64decode(data["image"])
        frame = cv2.imdecode(np.frombuffer(img_bytes, np.uint8), cv2.IMREAD_COLOR)

        result = DeepFace.analyze(frame, actions=["emotion"], enforce_detection=False)
        if isinstance(result, list):
            result = result[0]

        # Convert np.float32 to normal Python float
        raw_scores = result.get("emotion", {})
        scores = {k: float(v) for k, v in raw_scores.items()}

        return jsonify({
            "dominant_emotion": str(result.get("dominant_emotion", "neutral")),
            "emotion_scores": scores
        })

    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route("/health", methods=["GET"])
def health():
    return jsonify({"ok": True})

if __name__ == "__main__":
    app.run(debug=True)
