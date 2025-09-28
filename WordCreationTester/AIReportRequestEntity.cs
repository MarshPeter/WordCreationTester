
using System.ComponentModel.DataAnnotations;

namespace WordCreationTester
{
    public class AIReportRequestEntity
    {
        [Key] public Guid AIRequestId { get; set; }
        public Guid TenantId { get; set; }
        public required string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? Status { get; set; }              // e.g. Pending, Processing, Completed, Failed
        public string? StatusDescription { get; set; }   // Details of status/failure

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? ReportType { get; set; }
        public string? ReportCategories { get; set; } // JSON array
        public required string ReportStatements { get; set; } // JSON array
        public required string IndexType { get; set; }

        
        
    }

}
