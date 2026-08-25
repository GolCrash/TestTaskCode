using MongoDB.Driver;
using TestCode.Models;

namespace TestCode.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _db;
        
        public MongoDbContext(MongoDbSettings settings) 
        {
            MongoClient _client = new MongoClient(settings.ConnectionString);
            _db = _client.GetDatabase(settings.DatabaseName);
        }

        public IMongoDatabase Database => _db;
    }
}
