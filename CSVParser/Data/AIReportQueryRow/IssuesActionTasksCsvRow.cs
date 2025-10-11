namespace CsvParser.Data.AIReportQueryRow
{
    public class IssuesActionTasksCsvRow
    {
        public string? Title { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
        public string? Outcome { get; set; }
        public int? IATCategory { get; set; }
        public DateTime CreatedDt { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Location { get; set; }
        public string? Urgency { get; set; }
        public string? UrgencyDescription { get; set; }
        public string? Type { get; set; }
        public string? Allocated { get; set; }
    }
}