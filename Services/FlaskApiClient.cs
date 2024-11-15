using System.Text.Json;
using System.Text;
using TripMatch.Models;
using System.Net.Http.Headers;

namespace TripMatch.Services
{
    public class FlaskApiClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<FlaskApiClient> _logger;
        private const string BaseUrl = "http://127.0.0.1:5000";

        public FlaskApiClient(ILogger<FlaskApiClient> logger)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.Timeout = TimeSpan.FromSeconds(30);
            _logger = logger;
        }

        public async Task<string> PredictCluster(TravelPlans travelPlan)
        {
            try
            {
                _logger.LogInformation("Starting prediction request");

                if (travelPlan == null)
                {
                    throw new ArgumentNullException(nameof(travelPlan));
                }

                // Normalize data before sending
                travelPlan.NormalizeData();

                // Validate data format
                if (!travelPlan.ValidateDataFormat())
                {
                    throw new InvalidOperationException("Travel plan data format is invalid");
                }

                // Log normalized values
                _logger.LogInformation($"Normalized travel plan data:");
                _logger.LogInformation($"WantsToTravelTo: '{travelPlan.WantsToTravelTo}'");
                _logger.LogInformation($"WantsToLeaveOn: '{travelPlan.WantsToLeaveOn}'");
                _logger.LogInformation($"IsSpontanious: '{travelPlan.IsSpontanious}'");

                // Create request body with exact matching keys from training data
                var userData = new
                {
                    wantstotravelto = travelPlan.WantsToTravelTo,
                    wantstoleaveon = travelPlan.WantsToLeaveOn,
                    isspontanious = travelPlan.IsSpontanious
                };

                var jsonContent = JsonSerializer.Serialize(userData);
                _logger.LogInformation($"Sending prediction request with data: {jsonContent}");

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync($"{BaseUrl}/predict", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Response status code: {response.StatusCode}");
                _logger.LogInformation($"Response content: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Flask API returned {response.StatusCode}: {responseContent}");
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var predictionResponse = JsonSerializer.Deserialize<PredictionResponse>(responseContent, options);

                if (!string.IsNullOrEmpty(predictionResponse?.error))
                {
                    throw new Exception(predictionResponse.error);
                }

                if (string.IsNullOrEmpty(predictionResponse?.assigned_cluster))
                {
                    throw new Exception("No cluster assigned in response");
                }

                return predictionResponse.assigned_cluster;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting cluster");
                throw;
            }
        }

        private class PredictionResponse
        {
            public string? assigned_cluster { get; set; }
            public Dictionary<string, string>? user_data { get; set; }
            public string? error { get; set; }
        }
    }
}