import pandas as pd
from ml_utils import get_clustering_columns
import logging

# Configure logging
logging.basicConfig(level=logging.INFO,
                    format='%(asctime)s - %(levelname)s - %(message)s')


def normalize_value(value):
    """Normalize input values to match training data format."""
    if value is None:
        return None
    return str(value).lower().strip()


def find_best_cluster(new_user_data, centroids):
    """
    Find the exact matching cluster for a new user with improved validation and logging.

    Args:
        new_user_data: DataFrame row or Series containing user preferences
        centroids: DataFrame containing cluster centroids

    Returns:
        int: Matching cluster number or None if no match found
    """
    try:
        clustering_columns = get_clustering_columns()

        # Convert Series to DataFrame if necessary
        if isinstance(new_user_data, pd.Series):
            new_user_data = pd.DataFrame([new_user_data])

        # Validate input data
        logging.info(f"Input data: {new_user_data.to_dict('records')}")
        logging.info(f"Required columns: {clustering_columns}")

        # Check for missing columns
        missing_columns = set(clustering_columns) - set(new_user_data.columns)
        if missing_columns:
            logging.error(f"Missing required columns: {missing_columns}")
            return None

        # Normalize input data
        normalized_data = {}
        for col in clustering_columns:
            original_value = new_user_data[col].iloc[0]  # Get the first (and only) value
            normalized_value = normalize_value(original_value)
            normalized_data[col] = normalized_value
            logging.info(f"Column {col}: Original='{original_value}' → Normalized='{normalized_value}'")

        logging.info("Comparing with centroids:")
        for cluster_label, centroid in centroids.iterrows():
            logging.info(f"\nChecking cluster {cluster_label}:")
            exact_match = True

            for col in clustering_columns:
                user_value = normalized_data[col]
                centroid_value = normalize_value(centroid[col])

                logging.info(f"  {col}: User='{user_value}' vs Centroid='{centroid_value}'")

                if user_value != centroid_value:
                    exact_match = False
                    logging.info(f"  ❌ Mismatch on {col}")
                    break
                else:
                    logging.info(f"  ✓ Match on {col}")

            if exact_match:
                logging.info(f"✅ Found exact match in cluster {cluster_label}")
                return cluster_label

        logging.warning("❌ No exact match found in any cluster")
        return None

    except Exception as e:
        logging.error(f"Error in find_best_cluster: {str(e)}", exc_info=True)
        raise


class UserData:
    def __init__(self, **kwargs):
        self.validate_required_fields(kwargs)
        for key, value in kwargs.items():
            setattr(self, key, normalize_value(value))

    def validate_required_fields(self, data):
        required_fields = get_clustering_columns()
        missing_fields = [field for field in required_fields if field not in data]
        if missing_fields:
            raise ValueError(f"Missing required fields: {missing_fields}")

    def to_dataframe(self):
        clustering_columns = get_clustering_columns()
        data = {col: [getattr(self, col)] for col in clustering_columns if hasattr(self, col)}
        df = pd.DataFrame(data)
        logging.info(f"Created DataFrame: {df.to_dict('records')}")
        return df