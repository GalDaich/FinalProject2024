using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TripMatch.Services;
using TripMatch.Models;

namespace TripMatch.Pages
{
    public class ProfileModel : PageModel
    {
        private readonly MongoDBService _mongoDBService;
        private readonly ILogger<ProfileModel> _logger;

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
        [StringLength(100, MinimumLength = 6, ErrorMessage = "If provided, password must be at least 6 characters long")]
        public string? Password { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Date of birth is required")]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
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

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public ProfileModel(MongoDBService mongoDBService, ILogger<ProfileModel> logger)
        {
            _mongoDBService = mongoDBService;
            _logger = logger;
        }
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
        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                var user = await _mongoDBService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    var originalDateOfBirth = user.DateOfBirth;

                    FullName = user.FullName;
                    EmailAddress = user.EmailAddress;
                    PhoneNumber = user.PhoneNumber;
                    DateOfBirth = user.DateOfBirth;
                    LivesAt = user.LivesAt;
                    Hobby1 = user.Hobby1;
                    Hobby2 = user.Hobby2;
                }
                else
                {
                    return RedirectToPage("/Login");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile");
                ErrorMessage = "Error loading profile. Please try again.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                var currentUser = await _mongoDBService.GetUserByIdAsync(userId);
                if (currentUser == null)
                {
                    return RedirectToPage("/Login");
                }

                var originalDateOfBirth = currentUser.DateOfBirth;

                if (EmailAddress.ToLower() != currentUser.EmailAddress.ToLower())
                {
                    var existingUserWithEmail = await _mongoDBService.GetUserByEmailAsync(EmailAddress);
                    if (existingUserWithEmail != null && existingUserWithEmail.Id != userId)  // הוספת בדיקה שזה לא המשתמש עצמו
                    {
                        ModelState.AddModelError("EmailAddress", "This email is already in use");
                        DateOfBirth = originalDateOfBirth;
                        return Page();
                    }
                }

                if (PhoneNumber != currentUser.PhoneNumber)
                {
                    var existingUserWithPhone = await _mongoDBService.GetUserByPhoneNumberAsync(PhoneNumber);
                    if (existingUserWithPhone != null && existingUserWithPhone.Id != userId) // הוספת בדיקה שזה לא המשתמש עצמו
                    {
                        ModelState.AddModelError("PhoneNumber", "This phone number is already in use");
                        DateOfBirth = originalDateOfBirth;
                        return Page();
                    }
                }

                currentUser.FullName = FullName;
                currentUser.EmailAddress = EmailAddress;
                currentUser.PhoneNumber = PhoneNumber;
                currentUser.LivesAt = LivesAt;
                currentUser.Hobby1 = Hobby1;
                currentUser.Hobby2 = Hobby2;
                currentUser.DateOfBirth = originalDateOfBirth;

                if (!string.IsNullOrEmpty(Password))
                {
                    currentUser.Password = Password;
                }

                currentUser.UpdateAge();
                await _mongoDBService.UpdateUserAsync(currentUser);

                HttpContext.Session.SetString("UserName", currentUser.FullName);
                HttpContext.Session.SetString("UserEmail", currentUser.EmailAddress);

                SuccessMessage = "Profile updated successfully";
                DateOfBirth = originalDateOfBirth;
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error updating user profile - validation error");
                ErrorMessage = ex.Message;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                ErrorMessage = "Error updating profile. Please try again.";
                return Page();
            }
        }
    }
}