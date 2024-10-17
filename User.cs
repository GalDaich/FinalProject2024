using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace TripMatch
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("fullName")]
        public string FullName { get; set; }

        [BsonElement("emailAddress")]
        public string EmailAddress { get; set; }

        [BsonElement("password")]
        public string Password { get; set; }

        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; }

        [BsonElement("dateOfBirth")]
        public DateTime DateOfBirth { get; set; }

        [BsonElement("currentAge")]
        public int CurrentAge { get; set; }

        [BsonElement("livesAt")]
        public string LivesAt { get; set; }

        [BsonElement("additionalHobbies")]
        public List<string> AdditionalHobbies { get; set; }

        // Constructor to initialize collections
        public User()
        {
            AdditionalHobbies = new List<string>();
        }
    }
}