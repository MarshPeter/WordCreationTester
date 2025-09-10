using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvParser.Interfaces
{
    public interface ICSVExporter
    {
        Task<string> ExportCSV(string csvName, string timestamp, CancellationToken ct = default);
    }
}
