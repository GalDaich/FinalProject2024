using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using TripMatch.Services;

namespace TripMatch.Pages
{
    public class LoginModel : PageModel
    {
        private readonly MongoDBService _mongoDBService;
        private readonly ILogger<LoginModel> _logger;

        [BindProperty]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string EmailAddress { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public LoginModel(MongoDBService mongoDBService, ILogger<LoginModel> logger)
        {
            _mongoDBService = mongoDBService;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                var user = await _mongoDBService.GetUserByEmailAsync(EmailAddress);
                if (user == null || user.Password != Password)
                {
                    ErrorMessage = "Invalid email or password";
                    return Page();
                }

                // Set session
                HttpContext.Session.SetString("UserId", user.Id!);
                HttpContext.Session.SetString("UserName", user.FullName);
                HttpContext.Session.SetString("UserEmail", user.EmailAddress);

                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ErrorMessage = "An error occurred during login. Please try again.";
                return Page();
            }
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Index");
        }
    }
}