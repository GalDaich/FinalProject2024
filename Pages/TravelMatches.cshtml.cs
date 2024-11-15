using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TripMatch.Models;
using TripMatch.Services;

namespace TripMatch.Pages
{
    public class TravelMatchesModel : PageModel
    {
        private readonly MongoDBService _mongoDBService;
        private readonly ILogger<TravelMatchesModel> _logger;

        public List<MatchedTraveler> MatchedTravelers { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public int CurrentCluster { get; set; }

        public class MatchedTraveler
        {
            public string FullName { get; set; } = string.Empty;
            public int Age { get; set; }
            public string Location { get; set; } = string.Empty;
            public string TravelDestination { get; set; } = string.Empty;
            public string TravelMonth { get; set; } = string.Empty;
            public string IsSpontaneous { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string[] Hobbies { get; set; } = Array.Empty<string>();
            public bool IsFavorite { get; set; }
        }

        public TravelMatchesModel(MongoDBService mongoDBService, ILogger<TravelMatchesModel> logger)
        {
            _mongoDBService = mongoDBService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int cluster)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Login");
                }

                _logger.LogInformation($"Finding matches for cluster: {cluster}");
                CurrentCluster = cluster;

                var matchingPlans = await _mongoDBService.GetTravelPlansByClusterAsync(cluster);
                if (!matchingPlans.Any())
                {
                    _logger.LogInformation($"No matches found for cluster {cluster}");
                    return Page();
                }

                var currentUser = await _mongoDBService.GetUserByIdAsync(userId);
                if (currentUser == null)
                {
                    return RedirectToPage("/Login");
                }

                // Use HashSet to track unique phone numbers we've already processed
                var processedPhoneNumbers = new HashSet<string>();

                foreach (var plan in matchingPlans)
                {
                    // Skip if it's the current user's plan
                    if (plan.PhoneNumber == currentUser.PhoneNumber)
                    {
                        continue;
                    }

                    // Skip if we've already processed this phone number
                    if (processedPhoneNumbers.Contains(plan.PhoneNumber))
                    {
                        continue;
                    }

                    var user = await _mongoDBService.GetUserByPhoneNumberAsync(plan.PhoneNumber);
                    if (user != null)
                    {
                        var isFavorite = await _mongoDBService.IsFavoriteAsync(userId, plan.PhoneNumber);

                        MatchedTravelers.Add(new MatchedTraveler
                        {
                            FullName = user.FullName,
                            Age = user.CurrentAge,
                            Location = user.LivesAt,
                            TravelDestination = plan.WantsToTravelTo,
                            TravelMonth = plan.WantsToLeaveOn,
                            IsSpontaneous = plan.IsSpontanious,
                            PhoneNumber = user.PhoneNumber,
                            Hobbies = new[] { user.Hobby1, user.Hobby2 },
                            IsFavorite = isFavorite
                        });

                        // Add the phone number to our processed set
                        processedPhoneNumbers.Add(plan.PhoneNumber);
                    }
                }

                _logger.LogInformation($"Found {MatchedTravelers.Count} unique matching travelers");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding travel matches");
                ErrorMessage = "An error occurred while finding your travel matches.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostToggleFavoriteAsync(string phoneNumber)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Login");
                }

                var isFavorite = await _mongoDBService.IsFavoriteAsync(userId, phoneNumber);
                if (isFavorite)
                {
                    await _mongoDBService.RemoveFromFavoritesAsync(userId, phoneNumber);
                    SuccessMessage = "Removed from favorites.";
                }
                else
                {
                    await _mongoDBService.AddToFavoritesAsync(userId, phoneNumber);
                    SuccessMessage = "Added to favorites.";
                }

                return RedirectToPage(new { cluster = CurrentCluster });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite status");
                ErrorMessage = "An error occurred while updating favorites.";
                return RedirectToPage(new { cluster = CurrentCluster });
            }
        }
    }
}