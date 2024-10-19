import pandas as pd
import numpy as np
from sklearn.metrics import silhouette_score

import pandas as pd
import numpy as np
from sklearn.metrics import silhouette_score


def load_and_preprocess_data(file_path):
    """
    Load data from CSV and preprocess it.
    """
    try:
        # Try to load with '_id' as index
        df = pd.read_csv(file_path, index_col='_id')
    except ValueError:
        # If '_id' is not present, load without specifying an index
        df = pd.read_csv(file_path)

    # Ensure column names are standardized
    df.columns = df.columns.str.replace(' ', '').str.lower()

    # Add leading zero to phone number if it exists
    if 'phonenumber' in df.columns:
        df['phonenumber'] = '0' + df['phonenumber'].astype(str)

    # Drop rows with missing values
    df.dropna(inplace=True)

    return df


def get_clustering_columns():
    """
    Return the columns used for clustering.
    """
    return ['wantstotravelto', 'isspontanious', 'wantstoleaveon']


def calculate_silhouette_score(df_encoded, clusters):
    """
    Calculate the silhouette score for the clustering.
    """
    return silhouette_score(df_encoded, clusters, metric='euclidean')


def merge_small_clusters(df, cluster_column='cluster'):
    """
    Merge clusters that are too small.
    """
    cluster_sizes = df[cluster_column].value_counts()
    too_small = cluster_sizes[cluster_sizes < 10].index.tolist()
    if not too_small:
        return df  # No small clusters to merge
    for cluster in too_small:
        cluster_data = df[df[cluster_column] == cluster]
        if not cluster_data.empty:
            cluster_modes = cluster_data.mode().iloc[0]
            distances = []
            for target_cluster in cluster_sizes[cluster_sizes >= 10].index:
                target_data = df[df[cluster_column] == target_cluster]
                target_modes = target_data.mode().iloc[0]
                distance = (cluster_modes != target_modes).sum()
                distances.append((distance, target_cluster))
            if distances:
                nearest_cluster = min(distances, key=lambda x: x[0])[1]
                df.loc[df[cluster_column] == cluster, cluster_column] = nearest_cluster
    return df


def split_large_clusters(df, max_size, cluster_column='cluster', clustering_columns=None):
    """
    Split clusters that are too large.
    """
    from kmodes.kmodes import KModes

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
    if df[cluster_column].isnull().any():
        print("There are unclustered rows.")
    else:
        print("All rows are clustered.")


def normalize_cluster_labels(df, cluster_column='cluster'):
    """
    Normalize cluster labels to be consecutive integers.
    """
    unique_clusters = df[cluster_column].unique()
    cluster_mapping = {name: i for i, name in enumerate(unique_clusters)}
    df[cluster_column] = df[cluster_column].map(cluster_mapping)
    return df