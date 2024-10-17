using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TripMatch.Pages
{
    public class LoginModel : PageModel
    {
        private readonly MongoDBConnection _db;
        private readonly ILogger<LoginModel> _logger;

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public LoginModel(MongoDBConnection db, ILogger<LoginModel> logger)
        {
            _db = db;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            // אם המשתמש כבר מחובר, נעביר אותו לדף הבית
            if (HttpContext.Session.GetString("Username") != null)
            {
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostLogin()
        {
            _logger.LogInformation("Attempting to login with Email: {Email}", Email);

            // בדיקה שהשדות לא ריקים
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Please fill in all fields.";
                return Page();
            }

            var user = _db.Users.Find(u => u.EmailAddress.ToLower() == Email.ToLower()).FirstOrDefault();

            if (user == null)
            {
                _logger.LogWarning("Login failed: User with Email {Email} not found.", Email);
                ErrorMessage = "Login details are incorrect. Please try again.";
                return Page();
            }

            if (user.Password != Password)
            {
                _logger.LogWarning("Login failed: Incorrect password for Email {Email}.", Email);
                ErrorMessage = "Login details are incorrect. Please try again.";
                return Page();
            }

            // שמירת פרטי המשתמש ב-Session
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("UserId", user.Id.ToString());

            _logger.LogInformation("Login successful for Email: {Email}", Email);

            TempData["SuccessMessage"] = "Login successful!";

            return RedirectToPage("/Index");
        }
    }
}