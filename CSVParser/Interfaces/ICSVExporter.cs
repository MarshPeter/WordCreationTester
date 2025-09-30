

namespace CsvParser.Interfaces
{
    public interface ICSVExporter
    {
        Task<string> ExportCSV(string csvName, string timestamp, CancellationToken ct = default);
    }
}
