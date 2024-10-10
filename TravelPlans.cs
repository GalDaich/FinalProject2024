using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace TripMatch
{
    public class TravelPlans
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("personId")]
        public string? PersonId { get; set; }

        [BsonElement("fullName")]
        public string? FullName { get; set; }

        [BsonElement("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [BsonElement("dateOfBirth")]
        public DateTime DateOfBirth { get; set; }

        [BsonElement("currentAge")]
        public int CurrentAge { get; set; }

        [BsonElement("livesAt")]
        public string? LivesAt { get; set; }

        [BsonElement("additionalHobbies")]
        public AdditionalHobbies? AdditionalHobbies { get; set; }

        [BsonElement("wantsToTravelTo")]
        public string? WantsToTravelTo { get; set; }

        [BsonElement("wantsToLeaveOn")]
        public string? WantsToLeaveOn { get; set; }

        [BsonElement("wantedTrip")]
        public string? WantedTrip { get; set; }

        [BsonElement("preferredAccommodation")]
        public string? PreferredAccommodation { get; set; }

        [BsonElement("agesToMatchWith")]
        public string? AgesToMatchWith { get; set; }

        [BsonElement("shomerShabbat")]
        public string? ShomerShabbat { get; set; }

        [BsonElement("hasDriversLicense")]
        public string? HasDriversLicense { get; set; }

        [BsonElement("veganOrVegetarian")]
        public string? VeganOrVegetarian { get; set; }

        [BsonElement("isSpontaneous")]
        public string? IsSpontaneous { get; set; }

        [BsonElement("prefersNightTrips")]
        public string? PrefersNightTrips { get; set; }
    }

    public class AdditionalHobbies
    {
        [BsonElement("hobbies")]
        public List<string>? Hobbies { get; set; }
    }
}


