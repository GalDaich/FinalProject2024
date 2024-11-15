using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using TripMatch;
using TripMatch.Services;
using TripMatch.Models;


namespace TripMatch.Pages
{
    public class FutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime dateOfBirth)
            {
                if (dateOfBirth > DateTime.Today)
                {
                    return new ValidationResult("Please enter a valid date of birth");
                }
                if (dateOfBirth.Year <= 1900)
                {
                    return new ValidationResult("Please enter a valid date of birth");
                }
                return ValidationResult.Success;
            }
            return new ValidationResult("Date of birth is a required field");
        }
    }
    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime dateOfBirth)
            {
                if (dateOfBirth > DateTime.Today)
                {
                    return ValidationResult.Success;
                }

                var today = DateTime.Today;
                var age = today.Year - dateOfBirth.Year;

                if (dateOfBirth.Date > today.AddYears(-age))
                {
                    age--;
                }

                if (age >= _minimumAge)
                {
                    return ValidationResult.Success;
                }
            }

            return new ValidationResult(ErrorMessage ?? $"Minimum age required is {_minimumAge}");
        }
    }

    public class RegistrationModel : PageModel
    {
        private readonly MongoDBService _db;
        private readonly ILogger<RegistrationModel> _logger;

        [BindProperty]
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Date of birth is a required field")]
        [Display(Name = "Date of birth")]
        [DataType(DataType.Date)]
        [MinimumAge(18, ErrorMessage = "You must be at least 18 years old to register")]
        public DateTime DateOfBirth { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Location is required")]
        [Display(Name = "Location")]
        public string LivesAt { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "First hobby is required")]
        [Display(Name = "First Hobby")]
        public string Hobby1 { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Second hobby is required")]
        [Display(Name = "Second Hobby")]
        public string Hobby2 { get; set; }

        public string ErrorMessage { get; set; }
        public List<string> IsraeliCities { get; } = new List<string>
        {
            "Afula", "Arad", "Ariel", "Ashdod", "Ashkelon", "Bat Yam", "Be'er Sheva",
            "Beit She'an", "Beit Shemesh", "Bet Shemesh", "Bnei Brak", "Dimona",
            "Eilat", "Ganei Tikva", "Gedera", "Givatayim", "Hadera", "Haifa",
            "Herzliya", "Hod HaSharon", "Holon", "Jerusalem", "Karmiel", "Kfar Saba",
            "Kfar Yona", "Kiryat Ata", "Kiryat Bialik", "Kiryat Gat", "Kiryat Motzkin",
            "Kiryat Ono", "Kiryat Shmona", "Kiryat Yam", "Lod", "Ma'alot-Tarshiha",
            "Mevo Betar", "Migdal HaEmek", "Modi'in-Maccabim-Re'ut", "Nahariya",
            "Nazareth", "Nes Ziona", "Nesher", "Netanya", "Netivot", "Omer",
            "Or Yehuda", "Pardes Hanna-Karkur", "Petah Tikva", "Raanana", "Ramat Gan",
            "Ramat Hasharon", "Ramla", "Rehovot", "Rishon LeZion", "Rosh HaAyin",
            "Safed", "Sderot", "Tel Aviv", "Tiberias", "Tirat Carmel", "Tzfat",
            "Yavne", "Yehud", "Yokneam", "Zefat"
        };
        public RegistrationModel(MongoDBService db, ILogger<RegistrationModel> logger)
        {
            _db = db;
            _logger = logger;
            DateOfBirth = DateTime.Today;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (DateOfBirth > DateTime.Today)
                {
                    ModelState.AddModelError("DateOfBirth", "Please enter a valid date of birth");
                    return Page();
                }

                var age = DateTime.Today.Year - DateOfBirth.Year;
                if (DateOfBirth.Date > DateTime.Today.AddYears(-age))
                {
                    age--;
                }
                if (age < 18)
                {
                    ModelState.AddModelError("DateOfBirth", "You must be at least 18 years old to register");
                    return Page();
                }
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                // Check if email already exists
                var existingUserByEmail = await _db.GetUserByEmailAsync(EmailAddress);
                if (existingUserByEmail != null)
                {
                    ErrorMessage = "Email address is already registered";
                    return Page();
                }

                // check for existing phone number
                var existingUserByPhone = await _db.GetUserByPhoneNumberAsync(PhoneNumber);
                if (existingUserByPhone != null)
                {
                    ErrorMessage = "Phone number is already registered";
                    return Page();
                }

                if (DateOfBirth.Year <= 1900 || DateOfBirth.Year > DateTime.Today.Year)
                {
                    ModelState.AddModelError("DateOfBirth", "Please enter a valid date of birth");
                    return Page();
                }

                if (!ModelState.IsValid)
                {
                    return Page();
                }

                var user = new User
                {
                    FullName = FullName,
                    EmailAddress = EmailAddress,
                    Password = Password, 
                    PhoneNumber = PhoneNumber,
                    DateOfBirth = DateTime.SpecifyKind(DateOfBirth, DateTimeKind.Local), 
                    LivesAt = LivesAt,
                    Hobby1 = Hobby1,
                    Hobby2 = Hobby2
                };

                user.UpdateAge();
                user.NormalizeData();

                await _db.CreateUserAsync(user);
                _logger.LogInformation("User registered successfully: {Email}", EmailAddress);

                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetString("UserName", user.FullName);
                HttpContext.Session.SetString("UserEmail", user.EmailAddress);

                return RedirectToPage("/Index");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Registration failed - duplicate information");
                ErrorMessage = ex.Message;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
                ErrorMessage = "An error occurred during registration. Please try again.";
                return Page();
            }
        }
    }
}