import pandas as pd
import numpy as np
from sklearn.metrics import silhouette_score
import os
from kmodes.kmodes import KModes


def load_and_preprocess_data(file_path):
    """
    Load data from CSV and preprocess it.
    """
    try:
        print(f"Attempting to load file from: {file_path}")
        print(f"File exists: {os.path.exists(file_path)}")

        # Try to load with '_id' as index
        try:
            df = pd.read_csv(file_path, index_col='_id')
            print("Successfully loaded file with '_id' as index")
        except ValueError:
            print("Loading file without index specification")
            df = pd.read_csv(file_path)

        print(f"Loaded dataframe with shape: {df.shape}")

        # Convert all column names to lowercase and remove spaces
        df.columns = df.columns.str.lower().str.replace(' ', '')

        # Rename specific columns to match expected names
        column_mapping = {
            'wantstotr': 'wantstotravelto',
            'wantstole': 'wantstoleaveon',
            'isspontani': 'isspontanious'
        }
        df = df.rename(columns=column_mapping)

        # Ensure all relevant columns are lowercase for ML
        clustering_columns = get_clustering_columns()
        print(f"Checking for clustering columns: {clustering_columns}")

        for col in clustering_columns:
            if col in df.columns:
                df[col] = df[col].str.lower().str.strip()
                print(f"Processed column {col}")
            else:
                print(f"Warning: Expected column {col} not found in dataframe")

        print("Columns after preprocessing:", df.columns.tolist())

        # Drop rows with missing values
        initial_rows = len(df)
        df.dropna(inplace=True)
        final_rows = len(df)
        print(f"Dropped {initial_rows - final_rows} rows with missing values")

        return df

    except Exception as e:
        print(f"Error loading data: {str(e)}")
        print("Current DataFrame columns:", df.columns.tolist() if 'df' in locals() else "DataFrame not created yet")
        raise


def get_clustering_columns():
    """
    Return the columns used for clustering.
    """
    return ['wantstotravelto', 'isspontanious', 'wantstoleaveon']  # Kept original spelling


def calculate_silhouette_score(df_encoded, clusters):
    """
    Calculate the silhouette score for the clustering.
    """
    return silhouette_score(df_encoded, clusters, metric='euclidean')


def merge_small_clusters(df, cluster_column='cluster', min_size=10):
    """
    Merge clusters that are too small.
    """
    cluster_sizes = df[cluster_column].value_counts()
    too_small = cluster_sizes[cluster_sizes < min_size].index.tolist()

    if not too_small:
        return df  # No small clusters to merge

    clustering_columns = get_clustering_columns()

    for cluster in too_small:
        cluster_data = df[df[cluster_column] == cluster]
        if not cluster_data.empty:
            cluster_modes = cluster_data.mode().iloc[0]
            distances = []

            for target_cluster in cluster_sizes[cluster_sizes >= min_size].index:
                target_data = df[df[cluster_column] == target_cluster]
                target_modes = target_data.mode().iloc[0]
                # Calculate distance using only clustering columns
                distance = sum(cluster_modes[col] != target_modes[col] for col in clustering_columns)
                distances.append((distance, target_cluster))

            if distances:
                nearest_cluster = min(distances, key=lambda x: x[0])[1]
                df.loc[df[cluster_column] == cluster, cluster_column] = nearest_cluster

    return df


def split_large_clusters(df, max_size, cluster_column='cluster', clustering_columns=None):
    """
    Split clusters that are too large.
    """
    if clustering_columns is None:
        clustering_columns = get_clustering_columns()

    cluster_sizes = df[cluster_column].value_counts()
    too_large = cluster_sizes[cluster_sizes > max_size].index.tolist()

    if not too_large:
        return df  # No large clusters to split

    for cluster in too_large:
        cluster_data = df[df[cluster_column] == cluster]
        num_sub_clusters = int(np.ceil(len(cluster_data) / (max_size * 0.75)))

        if num_sub_clusters > 1:
            sub_k = KModes(n_clusters=num_sub_clusters, init='Huang', n_init=5, verbose=1)
            sub_clusters = sub_k.fit_predict(cluster_data[clustering_columns])
            new_labels = [f"{cluster}_{i}" for i in range(num_sub_clusters)]
            label_mapping = {i: new_labels[i] for i in range(len(new_labels))}
            df.loc[cluster_data.index, cluster_column] = [label_mapping[x] for x in sub_clusters]

    return df


def verify_clusters(df, cluster_column='cluster'):
    """
    Verify that all rows are clustered.
    """
    unclustered = df[cluster_column].isnull().sum()
    if unclustered > 0:
        print(f"Warning: Found {unclustered} unclustered rows.")
    else:
        print("Success: All rows are clustered.")

    cluster_counts = df[cluster_column].value_counts()
    print(f"\nCluster distribution:")
    print(cluster_counts)
    return unclustered == 0


def normalize_cluster_labels(df, cluster_column='cluster'):
    """
    Normalize cluster labels to be consecutive integers starting from 0.
    """
    unique_clusters = sorted(df[cluster_column].unique())
    cluster_mapping = {name: i for i, name in enumerate(unique_clusters)}
    df[cluster_column] = df[cluster_column].map(cluster_mapping)
    print(f"Normalized {len(unique_clusters)} clusters to range 0-{len(unique_clusters) - 1}")
    return df