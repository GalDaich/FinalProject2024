import pandas as pd
from ml_utils import get_clustering_columns


def find_best_cluster(new_user_data, centroids):
    """Find the closest cluster for a new user."""
    clustering_columns = get_clustering_columns()
    min_distance = float('inf')
    best_cluster = None

    for cluster_label, centroid in centroids.iterrows():
        distance = sum(new_user_data[col] != centroid[col] for col in clustering_columns)
        if distance < min_distance:
            min_distance = distance
            best_cluster = cluster_label

    return best_cluster


class UserData:
    def __init__(self, **kwargs):
        for key, value in kwargs.items():
            setattr(self, key, value)

    def to_dataframe(self):
        clustering_columns = get_clustering_columns()
        data = {col: [getattr(self, col)] for col in clustering_columns if hasattr(self, col)}
        return pd.DataFrame(data)
