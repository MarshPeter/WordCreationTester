using System;
using System.Collections.Generic;

namespace WordCreationTester.Models
{
    public enum IndexType { Assurance, Improvement, Compliance }

    public class ReportFilters
    {
        public bool? IncludeFairUsePolicy { get; set; }
        public bool? IncludeAssuranceCoverage { get; set; }
        public bool? IncludeRSATrends { get; set; }
        public List<string>? Categories { get; set; }
    }

    public class ReportParameters
    {
        public string ReportType { get; set; } = string.Empty;
        public ReportFilters? Filters { get; set; }
    }

    public class ReportStatement
    {
        public string StatementId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class AIReportRequestPayload
    {
        public Guid TenantId { get; set; }
        public Guid AIRequestId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public ReportParameters Parameters { get; set; } = new();
        public List<ReportStatement> ReportStatements { get; set; } = new();

        public bool IncludeAttachment { get; set; }
        public string? AttachmentId { get; set; }
        public Uri? AttachmentUrl { get; set; }

        public IndexType IndexType { get; set; } = IndexType.Assurance;
        public string PayloadVersion { get; set; } = "1.0";

        public void Validate()
        {
            if (TenantId == Guid.Empty) throw new ArgumentException("TenantId is required");
            if (AIRequestId == Guid.Empty) throw new ArgumentException("AIRequestId is required");
            if (string.IsNullOrWhiteSpace(CreatedBy)) throw new ArgumentException("CreatedBy is required");
            if (DateFrom == default || DateTo == default) throw new ArgumentException("Date range is required");
            if (DateFrom > DateTo) throw new ArgumentException("DateFrom cannot be after DateTo");
            if (Parameters is null) throw new ArgumentException("Parameters are required");
            if (string.IsNullOrWhiteSpace(Parameters.ReportType)) throw new ArgumentException("Parameters.ReportType is required");
            if (ReportStatements is null || ReportStatements.Count == 0)
                throw new ArgumentException("At least one ReportStatement is required");
            if (IncludeAttachment && AttachmentUrl is null && string.IsNullOrWhiteSpace(AttachmentId))
                throw new ArgumentException("IncludeAttachment is true but no AttachmentId/AttachmentUrl was provided");
        }
    }

    // Minimal message for the queue
    public class ServiceBusRequestMessage
    {
        public Guid TenantId { get; set; }
        public Guid AIRequestId { get; set; }
    }
}
