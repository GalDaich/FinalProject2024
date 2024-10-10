using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace TripMatch
{
    public class MongoDBConnection
    {
        private readonly IMongoDatabase _database;

        public MongoDBConnection(IConfiguration config)
        {
            var connectionString = config.GetSection("ConnectionStrings:ConnectionString").Value;
            var databaseName = config.GetSection("ConnectionStrings:DatabaseName").Value;

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException("ConnectionString or DatabaseName is missing in configuration");
            }

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    }
}
