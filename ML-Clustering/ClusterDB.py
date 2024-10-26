import pandas as pd
import numpy as np
from kmodes.kmodes import KModes
import pickle
from pymongo import MongoClient
from ml_utils import (
    load_and_preprocess_data, get_clustering_columns, calculate_silhouette_score,
    merge_small_clusters, split_large_clusters, verify_clusters, normalize_cluster_labels
)


def save_to_mongodb(df):
    """
    Save the clustered data to MongoDB
    """
    try:
        # Connect to MongoDB
        client = MongoClient('mongodb://localhost:27017/')

        # Access or create database
        db = client['TripMatch']

        # Access or create collection
        collection = db['ClusteredUsers']

        # Convert DataFrame to dictionary and save to MongoDB
        records = df.to_dict('records')

        # Remove existing records
        collection.delete_many({})

        # Insert new records
        collection.insert_many(records)

        print(f"Successfully saved {len(records)} records to MongoDB")

    except Exception as e:
        print(f"Error saving to MongoDB: {str(e)}")
    finally:
        client.close()


def train_model(file_path, k=200, max_size=60, iteration_limit=50, additional_rounds=5):
    # Load and preprocess data
    df = load_and_preprocess_data(file_path)

    # Get clustering columns
    clustering_columns = get_clustering_columns()
    df_cluster = df[clustering_columns]

    # Initial clustering
    km = KModes(n_clusters=k, init='Huang', n_init=20, verbose=1)
    clusters = km.fit_predict(df_cluster)
    df['cluster'] = clusters

    # Calculate silhouette score
    df_encoded = pd.get_dummies(df_cluster)
    score = calculate_silhouette_score(df_encoded, clusters)
    print("Silhouette Score: ", score)

    # Iterative merging and splitting
    for iteration in range(iteration_limit):
        old_cluster_count = df['cluster'].nunique()
        df = merge_small_clusters(df, 'cluster')
        df = split_large_clusters(df, max_size, 'cluster', clustering_columns)
        new_cluster_count = df['cluster'].nunique()
        if old_cluster_count == new_cluster_count:
            break

    # Additional splitting for large clusters
    for _ in range(additional_rounds):
        df = split_large_clusters(df, max_size, 'cluster', clustering_columns)

    # Verify clusters
    verify_clusters(df)

    # Normalize cluster labels
    df = normalize_cluster_labels(df)

    # Save to CSV
    df.to_csv('clustered_data.csv', index=False)

    # Save to MongoDB
    save_to_mongodb(df)

    print(df['cluster'].value_counts())
    print("Total number of clusters:", df['cluster'].nunique())

    return df, km


def save_model(model, file_path):
    with open(file_path, 'wb') as f:
        pickle.dump(model, f)


def load_model(file_path):
    with open(file_path, 'rb') as f:
        return pickle.load(f)


if __name__ == '__main__':
    df, km = train_model('MLFP20000PPLFILTERED.csv')
    save_model(km, 'kmodes_model.pkl')
