using System;
using System.Collections.Generic;

namespace WordCreationTester
{
    public class AIReportRequestPayload
    {
        public Guid TenantId { get; set; }
        public Guid AIRequestId { get; set; }
        public required string CreatedBy { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? ReportType { get; set; }
        public List<string>? ReportCategories { get; set; }
        public required List<ReportStatement> ReportStatements { get; set; }
        //public bool IncludeAttachment { get; set; }
        //public string AttachmentId { get; set; }
        //public Uri AttachmentUrl { get; set; }
        public required string IndexType { get; set; }
    }

    public class ReportStatement
    {
        public required string Text { get; set; }
    }

    public class ServiceBusRequestMessage
    {
        public Guid TenantId { get; set; }
        public Guid AIRequestId { get; set; }
    }
}
