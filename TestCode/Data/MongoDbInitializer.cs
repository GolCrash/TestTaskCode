using MongoDB.Bson;

namespace TestCode.Data
{
    public class MongoDbInitializer
    {
        protected readonly MongoDbContext _context;
        public MongoDbInitializer(MongoDbContext context)
        {
            _context = context;
        }

        public async Task InitializeAsync()
        {
            await _context.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

            Console.WriteLine("MongoDB подключена");
        }
    }
}
