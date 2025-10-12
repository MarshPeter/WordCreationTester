#nullable disable
using CSVParser.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvParser.Data.Models
{
    public partial class AIReportIndexes
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

        public virtual ICollection<AIReportRequest> AIReportRequests { get; set; } =
    new List<AIReportRequest>();
    }
}
