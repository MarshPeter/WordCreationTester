#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvParser.Data.Models
{
    public partial class AICSVDocuments
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public DateTime LastUpdatedTimestamp { get; set; }
        public ICollection<AIReportTenantIndexes> ReportTenantIndexes { get; set; }
    }
}
