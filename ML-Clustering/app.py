from flask import Flask, request, jsonify
from flask_cors import CORS
import pandas as pd
from ml_utils import load_and_preprocess_data, get_clustering_columns
from ClusterDB import train_model, save_model, load_model
from AddNewUsertoCluster import find_best_cluster, UserData
import os
import traceback

app = Flask(__name__)
CORS(app, resources={
    r"/predict": {
        "origins": ["http://localhost:44357"],
        "methods": ["POST"],
        "allow_headers": ["Content-Type"]
    }
})

# Global variables for model and data
km_model = None
clustered_data = None
centroids = None


def load_data_and_model():
    global km_model, clustered_data, centroids
    try:
        if not os.path.exists('kmodes_model.pkl'):
            print("Model file 'kmodes_model.pkl' not found. Please train the model first.")
            return False

        km_model = load_model('kmodes_model.pkl')
        print("Model loaded successfully")

        if not os.path.exists('clustered_data.csv'):
            print("Data file 'clustered_data.csv' not found. Please train the model first.")
            return False

        clustered_data = load_and_preprocess_data('clustered_data.csv')
        clustering_columns = get_clustering_columns()
        centroids = clustered_data.groupby('cluster')[clustering_columns].agg(lambda x: x.mode().iloc[0])
        print("Model and data loaded successfully")
        print(f"Centroids shape: {centroids.shape}")
        print(f"Available clusters: {centroids.index.tolist()}")
        print("\nSample of centroids data:")
        print(centroids.head())
        return True
    except Exception as e:
        print(f"Error loading model or data: {str(e)}")
        print(f"Traceback: {traceback.format_exc()}")
        return False


# Load data and model when the app starts
load_data_and_model()


@app.route('/predict', methods=['POST'])
def predict():
    try:
        print("\n--- New Prediction Request ---")
        print(f"Request Headers: {dict(request.headers)}")
        print(f"Request Data: {request.get_data(as_text=True)}")

        if km_model is None or clustered_data is None or centroids is None:
            print("Model not trained")
            return jsonify({"error": "Model not trained. Please train the model first."}), 400

        user_data = request.json
        print(f"Parsed user data: {user_data}")

        if not user_data:
            print("Invalid input data")
            return jsonify({"error": "Invalid input"}), 400

        required_fields = ['wantstotravelto', 'isspontanious', 'wantstoleaveon']
        for field in required_fields:
            if field not in user_data:
                print(f"Missing required field: {field}")
                return jsonify({"error": f"Missing required field: {field}"}), 400

        new_user = UserData(**user_data)
        cluster_df = new_user.to_dataframe()

        print(f"Created DataFrame: {cluster_df.to_dict('records')}")
        print(f"DataFrame columns: {cluster_df.columns.tolist()}")

        # Print some centroids data for comparison
        print("\nFirst few centroids for comparison:")
        print(centroids[get_clustering_columns()].head())

        assigned_cluster = find_best_cluster(cluster_df.iloc[0], centroids)

        if assigned_cluster is None:
            print("No exact match found in centroids")
            return jsonify({
                "error": "No exact match found for these preferences. Please verify your input matches the training data exactly."
            }), 400

        print(f"Assigned cluster: {assigned_cluster}")

        # Print the matching centroid data
        if assigned_cluster is not None:
            print("\nMatching centroid data:")
            print(centroids.loc[assigned_cluster])

        response_data = {
            "assigned_cluster": str(int(assigned_cluster)) if assigned_cluster is not None else None,
            "user_data": user_data
        }
        print(f"Sending response: {response_data}")

        return jsonify(response_data), 200

    except Exception as e:
        print(f"Error in prediction: {str(e)}")
        print(f"Traceback: {traceback.format_exc()}")
        return jsonify({"error": str(e)}), 500


if __name__ == '__main__':
    app.run(debug=True)