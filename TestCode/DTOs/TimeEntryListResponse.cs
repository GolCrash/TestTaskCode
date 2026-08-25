namespace TestCode.DTOs
{
    public class TimeEntryListResponse
    {
        public List<TimeEntryResponse> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPage { get; set; }
    }
}
