using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;


namespace TripMatch.Pages.Shared
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

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostLogin()
        {
            _logger.LogInformation("Attempting to login with Email: {Email}", Email);

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

            _logger.LogInformation("Login successful for Email: {Email}", Email);

            return RedirectToPage("/Index");
        }


    }
}
