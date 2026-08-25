namespace TestCode.DTOs.Reports
{
    public class ProjectReportResponse
    {
        public string ProjectId { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }

        public decimal Hours { get; set; }
        public decimal Amount { get; set; }
        public decimal Budget { get; set; }

        public decimal Percent { get; set; }

        public bool Overspent { get; set; }
        public bool Risk { get; set; }
    }
}