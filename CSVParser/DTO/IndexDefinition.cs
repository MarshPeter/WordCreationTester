using CsvParser.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvParser.DTO
{
    public record IndexDefinition(
        string IndexName,
        string DisplayName,
        string IndexDescription,
        ICSVExporter ExportService
    );
}
