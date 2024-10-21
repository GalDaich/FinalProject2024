using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using TripMatch;

namespace TripMatch.Pages
{
    public class ProfileModel : PageModel
    {
        private readonly MongoDBConnection _mongoDBConnection;

        public ProfileModel(MongoDBConnection mongoDBConnection)
        {
            _mongoDBConnection = mongoDBConnection;
        }

        [BindProperty]
        [Required(ErrorMessage = "Username is a required field")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be at least 3 characters long")]
        public string Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Email is a required field")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [BindProperty]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "If provided, password must be at least 6 characters long")]
        public string? Password { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Login");
            }

            var user = await _mongoDBConnection.Users
                .Find(u => u.Username == username)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                Username = user.Username;
                Email = user.EmailAddress;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Username) || Username.Length < 3)
            {
                ModelState.AddModelError("Username", "Username must be at least 3 characters long");
                return Page();
            }

            if (string.IsNullOrEmpty(Email) || !new EmailAddressAttribute().IsValid(Email))
            {
                ModelState.AddModelError("Email", "Valid email address is required");
                return Page();
            }

            var currentUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(currentUsername))
            {
                return RedirectToPage("/Login");
            }

            // Check if the new username already exists (only if username is being changed)
            if (Username != currentUsername)
            {
                var existingUser = await _mongoDBConnection.Users
                    .Find(u => u.Username == Username)
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "This username is already taken");
                    return Page();
                }
            }

            var user = await _mongoDBConnection.Users
                .Find(u => u.Username == currentUsername)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                var updateDefinition = Builders<User>.Update
                    .Set(u => u.Username, Username)
                    .Set(u => u.EmailAddress, Email);

                if (!string.IsNullOrEmpty(Password))
                {
                    if (Password.Length < 6)
                    {
                        ModelState.AddModelError("Password", "Password must be at least 6 characters long");
                        return Page();
                    }
                    updateDefinition = updateDefinition.Set(u => u.Password, Password);
                }

                var result = await _mongoDBConnection.Users.UpdateOneAsync(
                    u => u.Username == currentUsername,
                    updateDefinition
                );

                if (result.ModifiedCount > 0)
                {
                    if (currentUsername != Username)
                    {
                        HttpContext.Session.SetString("Username", Username);
                    }
                    SuccessMessage = "Profile updated successfully";
                }
                else
                {
                    ErrorMessage = "No changes were made to the profile";
                }
            }
            else
            {
                ErrorMessage = "User not found";
            }

            return Page();
        }
    }
}