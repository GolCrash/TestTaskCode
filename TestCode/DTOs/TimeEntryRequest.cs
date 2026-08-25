namespace TestCode.DTOs
{
    public class TimeEntryRequest
    {
        public string EmployeeId { get; set; }
        public string ProjectId { get; set; }
        public decimal Hours { get; set; }
        public DateTime Date { get; set; }
        public string Comment { get; set; }
        public int Version { get; set; }
    }
}
