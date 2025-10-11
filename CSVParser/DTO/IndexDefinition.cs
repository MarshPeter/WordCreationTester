using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CsvParser.DTO
{

    // Defines configuration for an Azure Cognitive Search index including name, description, query service, and row type
    public record IndexDefinition(
        string IndexName,
        string DisplayName,
        string IndexDescription,
        Type QueryServiceType,  
        Type RowType            
    );
}