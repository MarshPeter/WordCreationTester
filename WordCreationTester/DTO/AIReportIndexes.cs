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
        public string DisplayId { get; set; }
        public string IndexName { get; set; }
        public string IndexDescription { get; set; }
        public string UIDisplayName { get; set; }
        public DateTime IndexLastUpdatedDt { get; set; }
        public string CreatedById { get; set; }
        public DateTime CreatedDt { get; set; }
        public string Status { get; set; }
    }
}
