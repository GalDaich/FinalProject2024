using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using TripMatch.Models;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using CsvHelper;
using System.Globalization;
using static TripMatch.Models.User;

namespace TripMatch.Services
{
    public class MongoDBService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<TravelPlans> _travelPlansCollection;
        private readonly IMongoCollection<Favorite> _favoritesCollection;
        private readonly ILogger<MongoDBService> _logger;
        private readonly MongoClient _mongoClient;
        private readonly string _databaseName;
        private readonly FlaskApiClient _flaskApiClient;

        public MongoDBService(
            IConfiguration configuration,
            ILogger<MongoDBService> logger,
            FlaskApiClient flaskApiClient)
        {
            _logger = logger;
            _flaskApiClient = flaskApiClient;
            try
            {
                var connectionString = configuration["MongoDB:ConnectionString"];
                _databaseName = configuration["MongoDB:DatabaseName"];

                _logger.LogInformation($"Connecting to MongoDB at {connectionString}, database: {_databaseName}");

                _mongoClient = new MongoClient(connectionString);
                var mongoDatabase = _mongoClient.GetDatabase(_databaseName);

                _usersCollection = mongoDatabase.GetCollection<User>("Users");
                _travelPlansCollection = mongoDatabase.GetCollection<TravelPlans>("Travelplans");
                _favoritesCollection = mongoDatabase.GetCollection<Favorite>("Favorites");

                VerifyCollectionsAsync().Wait();

                _logger.LogInformation("MongoDB connection initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing MongoDB connection");
                throw;
            }
        }

        private void ImportUsersFromCsv(IMongoCollection<User> collection)
        {
            var csvPath = Path.Combine("ML-Clustering", "Users.csv");
            try
            {
                using var reader = new StreamReader(csvPath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                csv.Context.RegisterClassMap<UserMap>();

                csv.Context.Configuration.MissingFieldFound = null;
                csv.Context.Configuration.HeaderValidated = null;

                var records = csv.GetRecords<User>().ToList();
                if (records.Any())
                {
                    try
                    {
                        collection.InsertMany(records, new InsertManyOptions { IsOrdered = false });
                        _logger.LogInformation($"Successfully imported {records.Count} users");
                    }
                    catch (MongoBulkWriteException ex)
                    {
                        _logger.LogWarning($"Some users could not be imported: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import users from CSV");
                throw;
            }
        }

        private async Task VerifyCollectionsAsync()
        {
            try
            {
                var database = _mongoClient.GetDatabase(_databaseName);
                try
                {
                    _logger.LogInformation("Verifying MongoDB collections...");
                    var collections = await (await database.ListCollectionNamesAsync()).ToListAsync();

                    if (!collections.Contains("Users"))
                    {
                        _logger.LogInformation("Creating Users collection...");
                        await database.CreateCollectionAsync("Users");
                        ImportUsersFromCsv(_usersCollection);
                    }

                    if (!collections.Contains("Travelplans"))
                    {
                        _logger.LogInformation("Creating Travelplans collection...");
                        await database.CreateCollectionAsync("Travelplans");
                    }

                    if (!collections.Contains("Favorites"))
                    {
                        _logger.LogInformation("Creating Favorites collection...");
                        await database.CreateCollectionAsync("Favorites");
                    }

                    // Create indexes
                    await CreateIndexesAsync();
                    _logger.LogInformation("Collections verified successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accessing or creating collections");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying collections");
                throw;
            }
        }

        private async Task CreateIndexesAsync()
        {
            try
            {
                // Create user email and phone indexes
                var userIndexBuilder = Builders<User>.IndexKeys;
                var emailIndexModel = new CreateIndexModel<User>(
                    userIndexBuilder.Ascending(u => u.EmailAddress),
                    new CreateIndexOptions { Unique = true, Name = "EmailAddress_Unique" }
                );
                var phoneIndexModel = new CreateIndexModel<User>(
                    userIndexBuilder.Ascending(u => u.PhoneNumber),
                    new CreateIndexOptions { Unique = true, Name = "PhoneNumber_Unique" }
                );
                await _usersCollection.Indexes.CreateManyAsync(new[] { emailIndexModel, phoneIndexModel });

                // Create travel plans indexes
                var travelPlanIndexBuilder = Builders<TravelPlans>.IndexKeys;
                var phoneNumberIndex = new CreateIndexModel<TravelPlans>(
                    travelPlanIndexBuilder.Ascending(tp => tp.PhoneNumber),
                    new CreateIndexOptions { Name = "PhoneNumber_Index" }
                );
                var clusterIndex = new CreateIndexModel<TravelPlans>(
                    travelPlanIndexBuilder.Ascending(tp => tp.Cluster),
                    new CreateIndexOptions { Name = "Cluster_Index" }
                );
                await _travelPlansCollection.Indexes.CreateManyAsync(new[] { phoneNumberIndex, clusterIndex });

                // Create favorites indexes
                var favoriteIndexBuilder = Builders<Favorite>.IndexKeys;
                var userFavoriteIndex = new CreateIndexModel<Favorite>(
                    favoriteIndexBuilder.Ascending(f => f.UserId).Ascending(f => f.FavoritePhoneNumber),
                    new CreateIndexOptions { Unique = true, Name = "UserFavorite_Unique" }
                );
                await _favoritesCollection.Indexes.CreateManyAsync(new[] { userFavoriteIndex });

                _logger.LogInformation("Created all necessary indexes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Warning: Error creating MongoDB indexes");
            }
        }

        // User Methods
        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                // Check for duplicate email
                var existingUserByEmail = await _usersCollection
                    .Find(u => u.EmailAddress.ToLower() == user.EmailAddress.ToLower())
                    .FirstOrDefaultAsync();

                if (existingUserByEmail != null)
                {
                    _logger.LogWarning($"Attempted to create user with duplicate email: {user.EmailAddress}");
                    throw new InvalidOperationException("Email address is already registered");
                }

                // Check for duplicate phone number
                var existingUserByPhone = await _usersCollection
                    .Find(u => u.PhoneNumber == user.PhoneNumber)
                    .FirstOrDefaultAsync();

                if (existingUserByPhone != null)
                {
                    _logger.LogWarning($"Attempted to create user with duplicate phone number: {user.PhoneNumber}");
                    throw new InvalidOperationException("Phone number is already registered");
                }

                // Normalize user data before saving
                user.NormalizeData();

                await _usersCollection.InsertOneAsync(user);
                _logger.LogInformation($"Created user with ID: {user.Id}, Email: {user.EmailAddress}, Phone: {user.PhoneNumber}");

                return user;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                _logger.LogError(ex, $"Unexpected duplicate key error for user with email: {user.EmailAddress} and phone: {user.PhoneNumber}");
                throw new InvalidOperationException("User information already exists in the system", ex);
            }
            catch (InvalidOperationException)
            {
                // Rethrow InvalidOperationException without wrapping
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating user with email: {user.EmailAddress} and phone: {user.PhoneNumber}");
                throw new InvalidOperationException("An error occurred while creating the user account", ex);
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _usersCollection.Find(u => u.EmailAddress.ToLower() == email.ToLower())
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user with email: {email}");
                throw;
            }
        }


        public async Task<User?> GetUserByIdAsync(string id)
        {
            try
            {
                return await _usersCollection.Find(u => u.Id == id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user with ID: {id}");
                throw;
            }
        }

        public async Task UpdateUserAsync(User user)
        {
            try
            {
                // Get the existing user to check if phone number has changed
                var currentUser = await _usersCollection.Find(u => u.Id == user.Id).FirstOrDefaultAsync();
                if (currentUser == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                // Check for duplicate email (excluding current user)
                var existingUser = await _usersCollection.Find(u =>
                    u.EmailAddress.ToLower() == user.EmailAddress.ToLower() &&
                    u.Id != user.Id).FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    throw new InvalidOperationException("Email address is already in use by another account");
                }

                // Check if phone number has changed
                bool phoneNumberChanged = currentUser.PhoneNumber != user.PhoneNumber;
                if (phoneNumberChanged)
                {
                    // Check if new phone number is already in use
                    var existingUserWithPhone = await _usersCollection.Find(u =>
                        u.PhoneNumber == user.PhoneNumber &&
                        u.Id != user.Id).FirstOrDefaultAsync();

                    if (existingUserWithPhone != null)
                    {
                        throw new InvalidOperationException("Phone number is already in use by another account");
                    }

                    // Update phone number in TravelPlans collection
                    var travelPlansFilter = Builders<TravelPlans>.Filter.Eq(tp => tp.PhoneNumber, currentUser.PhoneNumber);
                    var travelPlansUpdate = Builders<TravelPlans>.Update.Set(tp => tp.PhoneNumber, user.PhoneNumber);
                    await _travelPlansCollection.UpdateManyAsync(travelPlansFilter, travelPlansUpdate);

                    // Update phone number in Favorites collection (where this user is favorited by others)
                    var favoritesFilter = Builders<Favorite>.Filter.Eq(f => f.FavoritePhoneNumber, currentUser.PhoneNumber);
                    var favoritesUpdate = Builders<Favorite>.Update.Set(f => f.FavoritePhoneNumber, user.PhoneNumber);
                    await _favoritesCollection.UpdateManyAsync(favoritesFilter, favoritesUpdate);

                    _logger.LogInformation($"Updated phone number from {currentUser.PhoneNumber} to {user.PhoneNumber} " +
                                         $"for user {user.Id} across all collections");
                }

                user.UpdateAge();
                var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
                var updateResult = await _usersCollection.ReplaceOneAsync(filter, user);

                if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
                {
                    throw new InvalidOperationException("User update failed");
                }

                _logger.LogInformation($"Updated user with ID: {user.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user with ID: {user.Id}");
                throw;
            }
        }

        public async Task DeleteUserAsync(string userId)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                // Delete user's travel plans
                await _travelPlansCollection.DeleteManyAsync(tp => tp.PhoneNumber == user.PhoneNumber);

                // Delete user's favorites
                await _favoritesCollection.DeleteManyAsync(f => f.UserId == userId);

                // Delete references to user in other users' favorites
                await _favoritesCollection.DeleteManyAsync(f => f.FavoritePhoneNumber == user.PhoneNumber);

                // Finally, delete the user
                var result = await _usersCollection.DeleteOneAsync(u => u.Id == userId);

                if (result.DeletedCount == 0)
                {
                    throw new InvalidOperationException("Failed to delete user");
                }

                _logger.LogInformation($"Successfully deleted user with ID: {userId} and all related data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user with ID: {userId}");
                throw;
            }
        }

        public async Task<User?> GetUserByPhoneNumberAsync(string phoneNumber)
        {
            try
            {
                return await _usersCollection.Find(u => u.PhoneNumber == phoneNumber)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user with phone number: {phoneNumber}");
                throw;
            }
        }

        // Travel Plan Methods
        public async Task<TravelPlans> CreateTravelPlanAsync(TravelPlans travelPlan)
        {
            try
            {
                _logger.LogInformation($"Starting travel plan creation process for phone: {travelPlan.PhoneNumber}");

                // Format strings exactly as they appear in the cluster training data
                travelPlan.WantsToLeaveOn = travelPlan.WantsToLeaveOn.Trim().ToLower();
                travelPlan.IsSpontanious = travelPlan.IsSpontanious.Trim().ToLower();

                _logger.LogInformation($"Formatted travel plan details: " +
                    $"To='{travelPlan.WantsToTravelTo}', " +
                    $"When='{travelPlan.WantsToLeaveOn}', " +
                    $"Spontaneous='{travelPlan.IsSpontanious}'");

                // Get cluster prediction before saving
                string predictedCluster;
                try
                {
                    predictedCluster = await _flaskApiClient.PredictCluster(travelPlan);
                    if (string.IsNullOrEmpty(predictedCluster))
                    {
                        throw new Exception("Received empty cluster prediction");
                    }
                    travelPlan.Cluster = int.Parse(predictedCluster);
                    _logger.LogInformation($"Received valid cluster prediction: {travelPlan.Cluster}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cluster prediction");
                    throw new Exception($"Failed to get cluster prediction: {ex.Message}");
                }

                // Save to MongoDB only if we have a valid cluster
                await _travelPlansCollection.InsertOneAsync(travelPlan);

                var savedPlan = await _travelPlansCollection.Find(tp => tp.Id == travelPlan.Id)
                    .FirstOrDefaultAsync();

                if (savedPlan == null)
                {
                    throw new Exception("Travel plan failed to save to database after successful cluster prediction");
                }

                _logger.LogInformation($"Successfully created travel plan. ID: {savedPlan.Id}, Cluster: {savedPlan.Cluster}");
                return savedPlan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateTravelPlanAsync");
                throw;
            }
        }

        public async Task<List<TravelPlans>> GetAllTravelPlansAsync()
        {
            try
            {
                return await _travelPlansCollection.Find(_ => true)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all travel plans");
                throw;
            }
        }

        public async Task<List<TravelPlans>> GetTravelPlansByClusterAsync(int clusterId)
        {
            try
            {
                _logger.LogInformation($"Searching for travel plans with cluster ID: {clusterId}");
                var plans = await _travelPlansCollection.Find(tp => tp.Cluster == clusterId).ToListAsync();
                _logger.LogInformation($"Found {plans.Count} travel plans in cluster {clusterId}");

                // Log the first few plans if any exist
                if (plans.Any())
                {
                    _logger.LogInformation("Sample plan details:");
                    foreach (var plan in plans.Take(3))
                    {
                        _logger.LogInformation($"Plan ID: {plan.Id}, Cluster: {plan.Cluster}, Phone: {plan.PhoneNumber}");
                    }
                }

                return plans;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving travel plans for cluster: {clusterId}");
                throw;
            }
        }

        public async Task<List<TravelPlans>> GetUserTravelPlansByPhoneAsync(string phoneNumber)
        {
            try
            {
                return await _travelPlansCollection.Find(tp => tp.PhoneNumber == phoneNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving travel plans for phone number: {phoneNumber}");
                throw;
            }
        }

        public async Task DeleteTravelPlanAsync(string travelPlanId)
        {
            try
            {
                var result = await _travelPlansCollection.DeleteOneAsync(tp => tp.Id == travelPlanId);
                if (result.DeletedCount == 0)
                {
                    throw new InvalidOperationException($"Travel plan with ID {travelPlanId} not found");
                }
                _logger.LogInformation($"Successfully deleted travel plan with ID: {travelPlanId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting travel plan with ID: {travelPlanId}");
                throw;
            }
        }

        // Favorites Methods
        public async Task AddToFavoritesAsync(string userId, string favoritePhoneNumber)
        {
            try
            {
                var favoriteUser = await GetUserByPhoneNumberAsync(favoritePhoneNumber);
                if (favoriteUser == null)
                {
                    throw new InvalidOperationException("Favorite user not found");
                }

                var existingFavorite = await _favoritesCollection
                    .Find(f => f.UserId == userId && f.FavoritePhoneNumber == favoritePhoneNumber)
                    .FirstOrDefaultAsync();

                if (existingFavorite == null)
                {
                    var favorite = new Favorite
                    {
                        UserId = userId,
                        FavoriteUserId = favoriteUser.Id,
                        FavoritePhoneNumber = favoritePhoneNumber
                    };

                    await _favoritesCollection.InsertOneAsync(favorite);
                    _logger.LogInformation($"Added favorite for user {userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding favorite");
                throw;
            }
        }

        public async Task RemoveFromFavoritesAsync(string userId, string favoritePhoneNumber)
        {
            try
            {
                var result = await _favoritesCollection.DeleteOneAsync(
                    f => f.UserId == userId && f.FavoritePhoneNumber == favoritePhoneNumber);

                if (result.DeletedCount == 0)
                {
                    throw new InvalidOperationException("Favorite not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing favorite");
                throw;
            }
        }

        public async Task<bool> IsFavoriteAsync(string userId, string favoritePhoneNumber)
        {
            try
            {
                var favorite = await _favoritesCollection
                    .Find(f => f.UserId == userId && f.FavoritePhoneNumber == favoritePhoneNumber)
                    .FirstOrDefaultAsync();
                return favorite != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking favorite status");
                throw;
            }
        }

        public async Task<List<(User User, TravelPlans LatestPlan)>> GetFavoritesAsync(string userId)
        {
            try
            {
                var favorites = await _favoritesCollection
                    .Find(f => f.UserId == userId)
                    .ToListAsync();

                var result = new List<(User User, TravelPlans LatestPlan)>();

                foreach (var favorite in favorites)
                {
                    var user = await GetUserByPhoneNumberAsync(favorite.FavoritePhoneNumber);
                    if (user != null)
                    {
                        var latestPlan = await _travelPlansCollection
                            .Find(tp => tp.PhoneNumber == favorite.FavoritePhoneNumber)
                            .SortByDescending(tp => tp.Id)
                            .FirstOrDefaultAsync();

                        result.Add((user, latestPlan));
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorites");
                throw;
            }
        }

        // Travel History Methods
        public async Task<List<(TravelPlans Plan, List<User> Matches)>> GetTravelHistoryAsync(string userId)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var userPlans = await _travelPlansCollection
                    .Find(tp => tp.PhoneNumber == user.PhoneNumber)
                    .SortByDescending(tp => tp.Id)
                    .ToListAsync();

                var result = new List<(TravelPlans Plan, List<User> Matches)>();

                foreach (var plan in userPlans)
                {
                    var matchingPlans = await _travelPlansCollection
                        .Find(tp => tp.Cluster == plan.Cluster && tp.PhoneNumber != user.PhoneNumber)
                        .ToListAsync();

                    var matchedUsers = new List<User>();
                    foreach (var matchPlan in matchingPlans)
                    {
                        var matchedUser = await GetUserByPhoneNumberAsync(matchPlan.PhoneNumber);
                        if (matchedUser != null)
                        {
                            matchedUsers.Add(matchedUser);
                        }
                    }

                    result.Add((plan, matchedUsers));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting travel history");
                throw;
            }
        }
        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");

    }
}