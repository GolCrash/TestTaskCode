namespace TestCode.Models
{
    public class Project
    {
        public string Id { get; set; }
        public string ProjectName { get; set; }
        public decimal Budget { get; set; }
        public DateTime ProjectStart { get; set; }
        public DateTime? ProjectEnd { get; set; }
    }
}
