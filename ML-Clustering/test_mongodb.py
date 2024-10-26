from pymongo import MongoClient

# Connect to MongoDB
client = MongoClient('mongodb://localhost:27017/')
db = client['TripMatch']
collection = db['ClusteredUsers']

# Print the count of documents
print(f"Number of documents: {collection.count_documents({})}")

# Print a sample document
print("\nSample document:")
print(collection.find_one())