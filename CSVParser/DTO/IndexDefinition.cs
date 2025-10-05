using CsvParser.Interfaces;


namespace CsvParser.DTO
    
    //Defines the structure of index for CSV export, including its name, display label, description, and the associated export service implementation
{
    public record IndexDefinition(
        string IndexName,                // This is hard to change on Azure once setup, choose a semantically meaningful name initially and stick with it
        string DisplayName,
        string IndexDescription,
        ICSVExporter ExportService
    );
}
