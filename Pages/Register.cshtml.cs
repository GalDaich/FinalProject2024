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
        [Required(ErrorMessage = "�� ����� ��� ��� ����")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "�� ������ ���� ����� ����� 3 �����")]
        public string Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "����� ���� ��� ��� ����")]
        [EmailAddress(ErrorMessage = "����� ���� �� �����")]
        public string Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "����� ��� ��� ����")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "������ ����� ����� ����� 6 �����")]
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
                // ����� �� �� ������ ��� ����
                var existingUsername = await _mongoDBConnection.Users
                    .Find(u => u.Username == Username)
                    .FirstOrDefaultAsync();

                if (existingUsername != null)
                {
                    ErrorMessage = "�� ������ ��� ���� ������";
                    return Page();
                }

                // ����� �� ����� ����� ��� �����
                var existingEmail = await _mongoDBConnection.Users
                    .Find(u => u.EmailAddress == Email)
                    .FirstOrDefaultAsync();

                if (existingEmail != null)
                {
                    ErrorMessage = "����� ����� ��� ����� ������";
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
                ErrorMessage = "����� ����� ��� ������. ��� ��� ��� ����� ����.";
                return Page();
            }
        }
    }
}