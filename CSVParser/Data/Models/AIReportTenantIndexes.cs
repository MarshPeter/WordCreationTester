using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvParser.Data.Models
{
    public partial class AIReportTenantIndexes
    {
        public Guid AIDocumentId { get; set; }
        public Guid AIReportIndexId { get; set; }

        public AICSVDocuments Document { get; set; }

        public AIReportIndexes Index {  get; set; }
    }
}
