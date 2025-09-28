
using System.ComponentModel.DataAnnotations;

namespace WordCreationTester
{
    public class AIReportStatusEntity
    {
        [Key]
        public int StatusId { get; set; }
        public Guid AIRequestId { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
