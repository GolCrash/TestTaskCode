using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TestCode.Models
{
    public class Project
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public decimal Budget { get; set; }
        public DateTime ProjectStart { get; set; }
        public DateTime? ProjectEnd { get; set; }
    }
}
