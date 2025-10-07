namespace CsvParser.DTO
{
    public record IndexDefinition(
        string IndexName,
        string DisplayName,
        string IndexDescription,
        Type QueryServiceType,  
        Type RowType            
    );
}