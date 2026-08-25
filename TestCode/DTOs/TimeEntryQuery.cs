namespace TestCode.DTOs
{
    public class TimeEntryQuery
    {
        public string EmployeeId { get; set; }
        public string ProjectId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
