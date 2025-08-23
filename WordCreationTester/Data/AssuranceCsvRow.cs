using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordCreationTester.Data
{
    // Expand with more a.* fields if you want them in the CSV.
    public class AssuranceCsvRow
    {
        public string A_Id { get; set; }
        public string Location { get; set; }
        public string UserName { get; set; }
        public string AssuranceTemplate { get; set; }
        public string AssuranceProgram { get; set; }
        public int? ProcessedYear { get; set; }
        public int? ProcessedMonth { get; set; }
        public string ProcessedDate { get; set; }

        public string QuestionText { get; set; }
        public string QuestionIdentifier { get; set; }
        public string Answer { get; set; }
        public string AnswerBGColor { get; set; }
        public string AnswerFGColor { get; set; }
        public string Comment { get; set; }

        // For structured branch we output r.Name; for unstructured it’s empty ("")
        public string RiskStandardActName { get; set; }
    }
}

