using System;
using System.Collections.Generic;

namespace WordCreationTester
{
    public class AIReportRequestPayload
    {
        public Guid TenantId { get; set; }
        public Guid AIRequestId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public ReportParameters Parameters { get; set; }
        public List<ReportStatement> ReportStatements { get; set; }
        public bool IncludeAttachment { get; set; }
        public string AttachmentId { get; set; }
        public Uri AttachmentUrl { get; set; }
        public string IndexType { get; set; }
    }

    public class ReportParameters
    {
        public string ReportType { get; set; }
        public ReportFilters Filters { get; set; }
    }

    public class ReportFilters
    {
        public List<string> Categories { get; set; }
    }

    public class ReportStatement
    {
        public string StatementId { get; set; }
        public string Text { get; set; }
    }

    public class ServiceBusRequestMessage
    {
        public Guid TenantId { get; set; }
        public Guid AIRequestId { get; set; }
    }
}
