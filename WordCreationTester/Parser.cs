
public class ReportSegment
{
    public required string Type { get; set; }
    public string? Text { get; set; }
    public List<string>? Items { get; set; }
    public List<string>? Columns { get; set; }
    public List<List<string>> Rows { get; set; }
}