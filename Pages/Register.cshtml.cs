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
        [Required(ErrorMessage = "שם משתמש הוא שדה חובה")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "שם המשתמש חייב להיות לפחות 3 תווים")]
        public string Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "כתובת מייל היא שדה חובה")]
        [EmailAddress(ErrorMessage = "כתובת מייל לא תקינה")]
        public string Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "סיסמה היא שדה חובה")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "הסיסמה חייבת להיות לפחות 6 תווים")]
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
                    ErrorMessage = "שם המשתמש כבר קיים במערכת";
                    return Page();
                }

                // בדיקה אם כתובת המייל כבר קיימת
                var existingEmail = await _mongoDBConnection.Users
                    .Find(u => u.EmailAddress == Email)
                    .FirstOrDefaultAsync();

                if (existingEmail != null)
                {
                    ErrorMessage = "כתובת המייל כבר קיימת במערכת";
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
                ErrorMessage = "אירעה שגיאה בעת ההרשמה. אנא נסה שוב מאוחר יותר.";
                return Page();
            }
        }
    }
}