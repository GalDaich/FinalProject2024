import pandas as pd
from ml_utils import get_clustering_columns


def find_best_cluster(new_user_data, centroids):
    """Find the closest cluster for a new user."""
    clustering_columns = get_clustering_columns()
    min_distance = float('inf')
    best_cluster = None

    # Look for exact matches (case-insensitive)
    for cluster_label, centroid in centroids.iterrows():
        matches = []
        for col in clustering_columns:
            if str(new_user_data[col]).lower() == str(centroid[col]).lower():
                matches.append(True)
            else:
                matches.append(False)

        if all(matches):
            return cluster_label

    # If no exact match, find best partial match
    for cluster_label, centroid in centroids.iterrows():
        distance = 0
        for col in clustering_columns:
            if str(new_user_data[col]).lower() != str(centroid[col]).lower():
                if col == 'wantstotravelto':
                    distance += 3  # Location mismatch weighs more
                else:
                    distance += 1

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
