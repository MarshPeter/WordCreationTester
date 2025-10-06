
using System.ComponentModel.DataAnnotations;

namespace WordCreationTester.DTO
{
    public class AIReportRequest
    {
        [Key] public Guid Id { get; set; }
        public required string DisplayId { get; set; }
        public required int ReportType { get; set; }
        public required DateTime FromDt { get; set; }
        public required DateTime ToDt { get; set; }
        public required string ReportParametersJSON { get; set; } // Received as JSON array
        public required string CreatedById { get; set; }
        public required DateTime CreatedDt { get; set; }
        public int? Status { get; set; }              // e.g. Pending, Processing, Completed, Failed
        public DateTime? OutcomeDt { get; set; }
        public string? Outcome {  get; set; }
        public Guid? AIReportIndexId { get; set;}
    }

}
