from flask import Flask, request, jsonify
import pandas as pd
from ml_utils import load_and_preprocess_data, get_clustering_columns
from ClusterDB import train_model, save_model, load_model
from AddNewUsertoCluster import find_best_cluster, UserData
import os

app = Flask(__name__)

# Global variables for model and data
km_model = None
clustered_data = None
centroids = None


def load_data_and_model():
    global km_model, clustered_data, centroids
    try:
        if not os.path.exists('kmodes_model.pkl'):
            print("Model file 'kmodes_model.pkl' not found. Please train the model first.")
            return

        km_model = load_model('kmodes_model.pkl')

        if not os.path.exists('clustered_data.csv'):
            print("Data file 'clustered_data.csv' not found. Please train the model first.")
            return

        clustered_data = load_and_preprocess_data('clustered_data.csv')
        clustering_columns = get_clustering_columns()
        centroids = clustered_data.groupby('cluster')[clustering_columns].agg(lambda x: x.mode().iloc[0])
        print("Model and data loaded successfully.")
    except Exception as e:
        print(f"Error loading model or data: {str(e)}")


# Load data and model when the app starts
load_data_and_model()


@app.route('/train', methods=['POST'])
def train():
    global km_model, clustered_data, centroids
    try:
        file_path = request.json.get('file_path', 'MLFP20000PPLFILTERED.csv')

        # Train the model
        df, km = train_model(file_path)

        # Save the model and data
        save_model(km, 'kmodes_model.pkl')
        df.to_csv('clustered_data.csv', index=False)

        # Update global variables
        km_model = km
        clustered_data = df
        clustering_columns = get_clustering_columns()
        centroids = clustered_data.groupby('cluster')[clustering_columns].agg(lambda x: x.mode().iloc[0])

        return jsonify({"message": "Model trained and saved successfully"}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route('/predict', methods=['POST'])
def predict():
    try:
        if km_model is None or clustered_data is None or centroids is None:
            return jsonify({"error": "Model not trained. Please train the model first."}), 400

        user_data = request.json
        if not user_data:
            return jsonify({"error": "Invalid input"}), 400

        new_user = UserData(**user_data)
        cluster_df = new_user.to_dataframe()
        assigned_cluster = find_best_cluster(cluster_df.iloc[0], centroids)

        return jsonify({
            "assigned_cluster": int(assigned_cluster),
            "user_data": user_data
        }), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route('/get_cluster_data', methods=['GET'])
def get_cluster_data():
    try:
        if clustered_data is None:
            return jsonify({"error": "Clustered data not available. Please train the model first."}), 400

        cluster_id = request.args.get('cluster_id', type=int)
        if cluster_id is None:
            return jsonify({"error": "Cluster ID is required"}), 400

        cluster_data = clustered_data[clustered_data['cluster'] == cluster_id].to_dict(orient='records')
        return jsonify({
            "cluster_id": cluster_id,
            "cluster_data": cluster_data
        }), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


if __name__ == '__main__':
    app.run(debug=True)
