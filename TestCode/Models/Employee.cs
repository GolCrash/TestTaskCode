namespace TestCode.Models
{
    public class Employee
    {
        public string Id { get; set; }
        public string LastName { get; set; }
        public string Name { get; set; }
        public string MiddleName { get; set; }
        public string Department { get; set; }
        public List<Rate> Rates { get; set; }
    }
}
