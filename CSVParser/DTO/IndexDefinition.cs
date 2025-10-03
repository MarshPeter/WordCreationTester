using CsvParser.Interfaces;


namespace CsvParser.DTO
    
    //Defines the structure of index for CSV export, including its name, display label, description, and the associated export service implementation
{
    public record IndexDefinition(
        string IndexName,
        string DisplayName,
        string IndexDescription,
        ICSVExporter ExportService
    );
}
