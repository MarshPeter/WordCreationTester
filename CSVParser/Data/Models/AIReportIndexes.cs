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
        public string IndexName { get; set; }
        public string IndexDescription { get; set; }
        public ICollection<AIReportTenantIndexes> ReportTenantIndexes { get; set; }
    }
}
