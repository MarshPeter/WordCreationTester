using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordCreationTester.DTO
{
    public class AIReportIndexes
    {
        public Guid Id { get; set; }
        public required string DisplayId { get; set; }
        public required string IndexName { get; set; }
        public required string IndexDescription { get; set; }
        public required string UIDisplayName { get; set; }
        public DateTime IndexLastUpdatedDt { get; set; }
        public required string CreatedById { get; set; }
        public DateTime CreatedDt { get; set; }
        public required string Status { get; set; }
    }
}
