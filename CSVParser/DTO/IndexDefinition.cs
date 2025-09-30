using CsvParser.Interfaces;


namespace CsvParser.DTO
{
    public record IndexDefinition(
        string IndexName,
        string DisplayName,
        string IndexDescription,
        ICSVExporter ExportService
    );
}
