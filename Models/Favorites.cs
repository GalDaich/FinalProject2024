using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TripMatch.Models
{
    public class Favorite
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("userId")]
        public string UserId { get; set; }

        [BsonElement("favoriteUserId")]
        public string FavoriteUserId { get; set; }

        [BsonElement("favoritePhoneNumber")]
        public string FavoritePhoneNumber { get; set; }

        [BsonElement("dateAdded")]
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}