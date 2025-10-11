namespace CsvParser.Data.AIReportQueryRow
{
    public class AssuranceCsvRow
    {
        public string? Location { get; set; }
        public string? UserName { get; set; }
        public string? AssuranceTemplate { get; set; }
        public string? AssuranceProgram { get; set; }
        public int ProcessedYear { get; set; }
        public int ProcessedMonth { get; set; }
        public string? QuestionText { get; set; }
        public string? QuestionIdentifier { get; set; }
        public string? Answer { get; set; }
        public string? AnswerBGColor { get; set; }
        public string? AnswerFGColor { get; set; }
        public string? Comment { get; set; }
        public string? RiskStandardActName { get; set; }
    }
}