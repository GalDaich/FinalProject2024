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
                var user = await _mongoDBService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return RedirectToPage("/Login");
                }

                // Store original email for comparison
                var originalEmail = user.EmailAddress;

                // Check if email is being changed and if it's already in use
                if (originalEmail.ToLower() != EmailAddress.ToLower())
                {
                    var existingUser = await _mongoDBService.GetUserByEmailAsync(EmailAddress);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("EmailAddress", "This email is already in use");
                        return Page();
                    }
                }

                // Update user object
                user.FullName = FullName;
                user.EmailAddress = EmailAddress;
                user.PhoneNumber = PhoneNumber;
                user.DateOfBirth = DateOfBirth;
                user.LivesAt = LivesAt;
                user.Hobby1 = Hobby1;
                user.Hobby2 = Hobby2;

                if (!string.IsNullOrEmpty(Password))
                {
                    user.Password = Password; // In production, use password hashing
                }

                user.UpdateAge(); // Update age based on date of birth

                await _mongoDBService.UpdateUserAsync(user);

                // Update session with new values
                HttpContext.Session.SetString("UserName", user.FullName);
                HttpContext.Session.SetString("UserEmail", user.EmailAddress);
                // Maintain the UserId in session
                HttpContext.Session.SetString("UserId", user.Id);

                SuccessMessage = "Profile updated successfully";
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error updating user profile - possibly duplicate email");
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