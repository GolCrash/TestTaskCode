using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TestCode.Models
{
    public class ClosedPeriod
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
    }
}