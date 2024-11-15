using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TripMatch.Models;
using TripMatch.Services;
using System.ComponentModel.DataAnnotations;

namespace TripMatch.Pages
{
    public class TravelPlanModel : PageModel
    {
        private readonly MongoDBService _mongoDBService;
        private readonly ILogger<TravelPlanModel> _logger;

        [BindProperty]
        [Required(ErrorMessage = "Please specify where you want to travel")]
        [Display(Name = "Where do you want to travel?")]
        public string WantsToTravelTo { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please specify when you want to leave")]
        [Display(Name = "When do you want to leave?")]
        public string WantsToLeaveOn { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please specify if you are spontaneous")]
        [Display(Name = "Are you spontaneous?")]
        public string IsSpontanious { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public TravelPlanModel(MongoDBService mongoDBService, ILogger<TravelPlanModel> logger)
        {
            _mongoDBService = mongoDBService;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Login");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                _logger.LogInformation("Starting OnPostAsync for travel plan creation");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid ModelState in travel plan creation");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning($"Validation error: {error.ErrorMessage}");
                    }
                    return Page();
                }

                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User not logged in during travel plan creation");
                    return RedirectToPage("/Login");
                }

                var user = await _mongoDBService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogError($"User not found for ID: {userId}");
                    ErrorMessage = "User not found";
                    return Page();
                }

                var travelPlan = new TravelPlans
                {
                    PhoneNumber = user.PhoneNumber,
                    WantsToTravelTo = WantsToTravelTo?.Trim() ?? string.Empty,
                    WantsToLeaveOn = WantsToLeaveOn?.Trim().ToLower() ?? string.Empty,
                    IsSpontanious = IsSpontanious?.Trim().ToLower() ?? string.Empty
                };

                _logger.LogInformation($"Creating travel plan with values: " +
                    $"To={travelPlan.WantsToTravelTo}, " +
                    $"When={travelPlan.WantsToLeaveOn}, " +
                    $"Spontaneous={travelPlan.IsSpontanious}");

                var createdPlan = await _mongoDBService.CreateTravelPlanAsync(travelPlan);

                if (createdPlan != null && createdPlan.Cluster >= 0)
                {
                    _logger.LogInformation($"Travel plan created successfully. ID: {createdPlan.Id}, Cluster: {createdPlan.Cluster}");
                    SuccessMessage = "Travel plan created successfully! We'll match you with compatible travel buddies.";
                    return RedirectToPage("/TravelMatches", new { cluster = createdPlan.Cluster });
                }
                else
                {
                    ErrorMessage = "Unable to create travel plan. Please try again.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating travel plan");
                ErrorMessage = "An error occurred while creating your travel plan. Please try again.";
                return Page();
            }
        }
    }
}