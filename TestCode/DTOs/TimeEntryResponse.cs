namespace TestCode.DTOs
{
    public class TimeEntryResponse
    {
        public string Id { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string ProjectId { get; set; }
        public string ProjectCode { get; set; }
        public decimal Hours { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Comment { get; set; }
        public bool IsOvertime { get; set; }
        public decimal Rate { get; set; }
        public int Version { get; set; }
    }
}
