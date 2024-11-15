using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TripMatch.Models;
using TripMatch.Services;

namespace TripMatch.Pages
{
    public class FavoritesModel : PageModel
    {
        private readonly MongoDBService _mongoDBService;
        private readonly ILogger<FavoritesModel> _logger;

        public List<(User User, TravelPlans LatestPlan)> Favorites { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public FavoritesModel(MongoDBService mongoDBService, ILogger<FavoritesModel> logger)
        {
            _mongoDBService = mongoDBService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Login");
                }

                Favorites = await _mongoDBService.GetFavoritesAsync(userId);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorites");
                ErrorMessage = "An error occurred while loading your favorites.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostRemoveFavoriteAsync(string phoneNumber)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Login");
                }

                await _mongoDBService.RemoveFromFavoritesAsync(userId, phoneNumber);
                SuccessMessage = "Successfully removed from favorites.";

                // Reload the favorites list
                Favorites = await _mongoDBService.GetFavoritesAsync(userId);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing favorite");
                ErrorMessage = "An error occurred while removing the favorite.";
                Favorites = await _mongoDBService.GetFavoritesAsync(HttpContext.Session.GetString("UserId") ?? string.Empty);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAddFavoriteAsync(string phoneNumber)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Login");
                }

                await _mongoDBService.AddToFavoritesAsync(userId, phoneNumber);
                SuccessMessage = "Successfully added to favorites.";

                // Reload the favorites list
                Favorites = await _mongoDBService.GetFavoritesAsync(userId);
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while adding favorite");
                ErrorMessage = ex.Message;
                Favorites = await _mongoDBService.GetFavoritesAsync(HttpContext.Session.GetString("UserId") ?? string.Empty);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding favorite");
                ErrorMessage = "An error occurred while adding the favorite.";
                Favorites = await _mongoDBService.GetFavoritesAsync(HttpContext.Session.GetString("UserId") ?? string.Empty);
                return Page();
            }
        }

        public async Task<JsonResult> OnGetIsFavoriteAsync(string phoneNumber)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return new JsonResult(new { success = false, error = "Not logged in" });
                }

                var isFavorite = await _mongoDBService.IsFavoriteAsync(userId, phoneNumber);
                return new JsonResult(new { success = true, isFavorite });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking favorite status");
                return new JsonResult(new { success = false, error = "Error checking favorite status" });
            }
        }
    }
}