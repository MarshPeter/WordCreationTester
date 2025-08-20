using System;
using System.ComponentModel.DataAnnotations;

namespace WordCreationTester
{
    public class AIReportRequestEntity
    {
        [Key] public Guid AIRequestId { get; set; }
        public Guid TenantId { get; set; }
        public string CreatedBy { get; set; }
        public string ParametersJson { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
