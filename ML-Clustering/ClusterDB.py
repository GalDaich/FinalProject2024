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
    Save the clustered data to MongoDB with verification and proper phone number formatting
    """
    try:
        # Connect to MongoDB
        client = MongoClient('mongodb://localhost:27017/')

        # Access or create database
        db = client['TripMatch']

        # Access or create collection
        collection = db['Travelplans']

        # Create a copy of the dataframe
        df_to_save = df.copy()

        # Ensure proper phone number formatting
        if 'phonenumber' in df_to_save.columns:
            # Format phone numbers to maintain leading zeros
            df_to_save['phonenumber'] = df_to_save['phonenumber'].astype(str).apply(lambda x: x.zfill(10))
        elif 'PhoneNumber' in df_to_save.columns:
            df_to_save['PhoneNumber'] = df_to_save['PhoneNumber'].astype(str).apply(lambda x: x.zfill(10))

        # Ensure cluster column is present
        if 'cluster' not in df_to_save.columns:
            raise ValueError("Cluster column is missing from the data")

        # Convert DataFrame to dictionary
        records = df_to_save.to_dict('records')

        # Verify records have cluster information
        if records and 'cluster' not in records[0]:
            raise ValueError("Cluster information is missing from records")

        # Remove existing records
        collection.delete_many({})

        # Insert new records
        collection.insert_many(records)

        # Verify insertion
        saved_count = collection.count_documents({})
        if saved_count != len(records):
            raise ValueError(f"Only {saved_count} records saved out of {len(records)}")

        print(f"Successfully saved {saved_count} records to MongoDB")

        # Sample verification
        sample = collection.find_one()
        if sample:
            print("Sample record verification:")
            print(f"Cluster value: {sample.get('cluster')}")
            print(f"Travel destination: {sample.get('wantstotravelto')}")
            print(f"Phone number: {sample.get('phonenumber') or sample.get('PhoneNumber')}")

    except Exception as e:
        print(f"Error saving to MongoDB: {str(e)}")
        raise  # Re-raise the exception to see full traceback
    finally:
        client.close()


def train_model(file_path, k=200, max_size=60, iteration_limit=50, additional_rounds=5):
    # Load and preprocess data
    df = load_and_preprocess_data(file_path)

    # Get clustering columns
    clustering_columns = get_clustering_columns()
    df_cluster = df[clustering_columns]

    # Initial clustering with 10 runs
    km = KModes(n_clusters=k, init='Huang', n_init=10, verbose=1)
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

    # Print cluster info before saving
    print("\nCluster information before saving:")
    print(df['cluster'].value_counts().head())
    print(f"Number of unique clusters: {df['cluster'].nunique()}")

    # Save to CSV
    df.to_csv('clustered_data.csv', index=False)

    # Save to MongoDB
    save_to_mongodb(df)

    return df, km


def save_model(model, file_path):
    with open(file_path, 'wb') as f:
        pickle.dump(model, f)


def load_model(file_path):
    with open(file_path, 'rb') as f:
        return pickle.load(f)


if __name__ == '__main__':
    df, km = train_model('Travelplans.csv')
    save_model(km, 'kmodes_model.pkl')