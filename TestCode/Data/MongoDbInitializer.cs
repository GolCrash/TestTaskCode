using MongoDB.Bson;

namespace TestCode.Data
{
    public class MongoDbInitializer
    {
        protected readonly MongoDbContext _context;
        protected readonly MongoDbIndexes _indexes;
        public MongoDbInitializer(MongoDbContext context, MongoDbIndexes indexes)
        {
            _context = context;
            _indexes = indexes;
        }

        public async Task InitializeAsync()
        {
            await _context.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            await _indexes.CreateIndexesAsync();

            Console.WriteLine("MongoDB подключена");
        }
    }
}
