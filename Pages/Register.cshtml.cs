using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using TripMatch;

namespace TripMatch.Pages
{
    public class RegistrationModel : PageModel
    {
        private readonly MongoDBConnection _mongoDBConnection;

        public RegistrationModel(MongoDBConnection mongoDBConnection)
        {
            _mongoDBConnection = mongoDBConnection;
        }

        [BindProperty]
        [Required(ErrorMessage = "Username is a required field")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be at least 3 characters")]
        public string Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Email address is a required field")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Password is a required field")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // בדיקה אם שם המשתמש כבר קיים
                var existingUsername = await _mongoDBConnection.Users
                    .Find(u => u.Username == Username)
                    .FirstOrDefaultAsync();

                if (existingUsername != null)
                {
                    ErrorMessage = "The username is already taken";
                    return Page();
                }

                // בדיקה אם כתובת המייל כבר קיימת
                var existingEmail = await _mongoDBConnection.Users
                    .Find(u => u.EmailAddress == Email)
                    .FirstOrDefaultAsync();

                if (existingEmail != null)
                {
                    ErrorMessage = "The email address is already registered";
                    return Page();
                }

                var user = new User
                {
                    Username = Username,
                    EmailAddress = Email,
                    Password = Password 
                };

                await _mongoDBConnection.Users.InsertOneAsync(user);
                HttpContext.Session.SetString("Username", user.Username);
                return RedirectToPage("/Index"); 
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred during registration. Please try again later.";
                return Page();
            }
        }
    }
}