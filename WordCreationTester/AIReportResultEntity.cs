using System;

namespace WordCreationTester
{
    public class AIReportResultEntity
    {
        public Guid ResultId { get; set; } = Guid.NewGuid();

        public Guid AIRequestId { get; set; }   // FK back to the request
        public Guid TenantId { get; set; }

        public string ReportName { get; set; }
        public string ReportBlobUrl { get; set; }   // blob storage link
        public string Status { get; set; }      // Completed / Failed / etc.

        public DateTime? CompletedAt { get; set; }
    }
}
