namespace TestCode.Models
{
    public class TimeEntry
    {
        public string EmployeeId { get; set; }
        public string ProjectId { get; set; }
        public DateTime Date { get; set; }
        public decimal Hours { get; set; }
        public string Comment { get; set; }
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
