from pymongo import MongoClient


def verify_mongodb_data():
    try:
        # Connect to MongoDB
        client = MongoClient('mongodb://localhost:27017/')

        # Access database
        db = client['TripMatch']

        # Access collection
        collection = db['ClusteredUsers']

        # Count documents
        count = collection.count_documents({})

        # Get sample document
        sample = collection.find_one()

        print(f"Found {count} documents in MongoDB")
        print("\nSample document:")
        print(sample)

    except Exception as e:
        print(f"Error verifying MongoDB data: {str(e)}")
    finally:
        client.close()


# This allows you to run the verification directly
if __name__ == "__main__":
    verify_mongodb_data()