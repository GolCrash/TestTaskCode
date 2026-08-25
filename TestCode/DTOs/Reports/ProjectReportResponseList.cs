namespace TestCode.DTOs.Reports
{
    public class ProjectReportResponseList
    {
        public List<ProjectReportResponse> Items { get; set; }

        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
    }
}