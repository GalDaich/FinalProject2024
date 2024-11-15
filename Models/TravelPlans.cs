using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace TripMatch.Models
{
    public class TravelPlans
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("phonenumber")]
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [BsonElement("wantstotravelto")]
        [Required]
        public string WantsToTravelTo { get; set; } = string.Empty;

        [BsonElement("wantstoleaveon")]
        [Required]
        public string WantsToLeaveOn { get; set; } = string.Empty;

        [BsonElement("isspontanious")]
        [Required]
        public string IsSpontanious { get; set; } = string.Empty;

        [BsonElement("cluster")]
        public int Cluster { get; set; }

        public void NormalizeData()
        {
            // Normalize all fields to match exactly with training data format
            WantsToLeaveOn = WantsToLeaveOn?.ToLower().Trim() ?? string.Empty;
            IsSpontanious = IsSpontanious?.ToLower().Trim() ?? string.Empty;

            // Special handling for WantsToTravelTo to ensure exact match with training data
            if (!string.IsNullOrEmpty(WantsToTravelTo))
            {
                WantsToTravelTo = WantsToTravelTo.Trim();

                // Ensure region names match exactly with training data format
                // Add any specific normalization rules based on your training data
                if (WantsToTravelTo.Contains("south america", StringComparison.OrdinalIgnoreCase))
                {
                    WantsToTravelTo = "south america - for example, brazil, argentina, chile, peru, colombia";
                }
                // Add similar rules for other regions...
            }
        }

        public bool ValidateDataFormat()
        {
            // Validate that data matches expected format
            if (string.IsNullOrEmpty(WantsToTravelTo) ||
                string.IsNullOrEmpty(WantsToLeaveOn) ||
                string.IsNullOrEmpty(IsSpontanious))
            {
                return false;
            }

            // Validate IsSpontanious is either "yes" or "no"
            if (IsSpontanious.ToLower() != "yes" && IsSpontanious.ToLower() != "no")
            {
                return false;
            }

            // Validate WantsToLeaveOn is a valid month
            var validMonths = new[] { "january", "february", "march", "april", "may", "june",
                                    "july", "august", "september", "october", "november", "december" };
            if (!validMonths.Contains(WantsToLeaveOn.ToLower()))
            {
                return false;
            }

            return true;
        }
    }
}