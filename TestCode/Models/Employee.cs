using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TestCode.Models
{
    public class Employee
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }
        public string LastName { get; set; }
        public string Name { get; set; }
        public string MiddleName { get; set; }
        public string Department { get; set; }
        public List<Rate> Rates { get; set; }
    }
}
