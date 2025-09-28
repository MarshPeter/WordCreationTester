

namespace WordCreationTester
{
    public class AIReportResultEntity
    {
        public Guid ResultId { get; set; } = Guid.NewGuid();

        public Guid AIRequestId { get; set; }   // FK back to the request
        public Guid TenantId { get; set; }

        public required string ReportName { get; set; }
        public required string ReportBlobUrl { get; set; }   // blob storage link
        public required string Status { get; set; }      // Completed / Failed / etc.

        public DateTime CompletedAt { get; set; }
    }
}
