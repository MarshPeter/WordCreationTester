using System;

namespace CsvParser.DTO
{
    public class ComplaintsOrComplimentsCsvRow
    {
        public string? Contact { get; set; }
        public string? ContactAddress { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Commentary { get; set; }
        public string? InitialResponse { get; set; }
        public string? Outcome { get; set; }
        public string? CreatedById { get; set; }
        public DateTime CreatedDt { get; set; }
        public string? Comment { get; set; }
    }
}