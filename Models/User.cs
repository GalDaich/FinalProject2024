using CsvHelper.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace TripMatch.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString(); // Initialize with new ID

        [BsonElement("fullName")]
        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = string.Empty;

        [BsonElement("emailAddress")]
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string EmailAddress { get; set; } = string.Empty;

        [BsonElement("password")]
        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [BsonElement("phoneNumber")]
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; } = string.Empty;

        [BsonElement("dateOfBirth")]
        [Required(ErrorMessage = "Date of birth is required")]
        public DateTime DateOfBirth { get; set; }

        [BsonElement("currentAge")]
        public int CurrentAge { get; set; }

        [BsonElement("livesAt")]
        [Required(ErrorMessage = "Location is required")]
        public string LivesAt { get; set; } = string.Empty;

        [BsonElement("hobby1")]
        [Required(ErrorMessage = "First hobby is required")]
        public string Hobby1 { get; set; } = string.Empty;

        [BsonElement("hobby2")]
        [Required(ErrorMessage = "Second hobby is required")]
        public string Hobby2 { get; set; } = string.Empty;

        public void UpdateAge()
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age))
            {
                age--;
            }
            CurrentAge = age;
        }

        // Add method to help maintain data consistency
        public void NormalizeData()
        {
            FullName = FullName?.Trim() ?? string.Empty;
            EmailAddress = EmailAddress?.ToLower().Trim() ?? string.Empty;
            PhoneNumber = PhoneNumber?.Trim() ?? string.Empty;
            LivesAt = LivesAt?.Trim() ?? string.Empty;
            Hobby1 = Hobby1?.Trim() ?? string.Empty;
            Hobby2 = Hobby2?.Trim() ?? string.Empty;
            UpdateAge();
        }
        public sealed class UserMap : ClassMap<User>
        {
            public UserMap()
            {
                Map(m => m.FullName).Name("fullName");
                Map(m => m.EmailAddress).Name("emailAddress");
                Map(m => m.Password).Name("password");
                Map(m => m.PhoneNumber).Name("phoneNumber");
                Map(m => m.DateOfBirth).Name("dateOfBirth");
                Map(m => m.CurrentAge).Name("currentAge");
                Map(m => m.LivesAt).Name("livesAt");
                Map(m => m.Hobby1).Name("hobby1");
                Map(m => m.Hobby2).Name("hobby2");

            }
        }
    }
}