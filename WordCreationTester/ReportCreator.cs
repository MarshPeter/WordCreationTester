using Newtonsoft.Json;
using Openize.Words;

namespace WordCreationTester
{
    public static class ReportCreator
    {
        public static void runGeneration(string aiJson)
        {
            string docsDirectory = "./docs";
            string filename = "Generated.docx";


            // Sanitize curly quotes into smart quotes in case AI decides to be difficult and disobey instructions
            string jsonString = aiJson
                .Replace('“', '"')
                .Replace('”', '"')
                .Replace('‘', '\'')
                .Replace('’', '\'');

            try
            {
                // Deserialize the report. If parsing returns null, default to an empty list .
                var report = JsonConvert.DeserializeObject<List<ReportSegment>>(jsonString) ?? new List<ReportSegment>();




                // Create a new Word document instance
                var doc = new Document();

                // Create a new WordFileGenerator instance, wrapping the Word document with helper methods (addTitle, addHeader, addParagraph, etc.)
                var word = new WordFileGenerator(doc);

                // Process each report segment one by one and add it to the Word doc
                foreach (ReportSegment s in report)
                {
                    ParseSection(word, s);
                }

                // Save the generated Word document to directory
                doc.Save($"{docsDirectory}/{filename}");
            }
            catch (System.Exception ex)
            {
                throw new FileFormatException("An error occurred.", ex);
            }

        }
        static void ParseSection(WordFileGenerator w, ReportSegment s)
        {
            // Add report title
            if (s.Type.Equals("title"))
            {
                if (s.Text == null || s.Text.Equals("")) return;

                w.addTitle(s.Text);
            }

            // Add header
            if (s.Type.Equals("header"))
            {
                if (s.Text == null || s.Text.Equals("")) return;

                w.addHeader(s.Text);
            }

            // Handle plain paragraph text
            if (s.Type.Equals("paragraph"))
            {
                if (s.Text == null || s.Text.Equals("")) return;

                w.addParagraph(s.Text);
            }

            // Handle bullet-point lists
            if (s.Type.Equals("list"))
            {
                if (s.Items == null || s.Items.Count == 0) return;

                foreach (string point in s.Items)
                {
                    w.addDotpointParagraph(point);
                }
            }

            // Handle tables
            if (s.Type.Equals("table"))
            {
                
                if (s.Columns == null || s.Columns.Count == 0 ||
                    s.Rows == null || s.Rows.Count == 0)
                {
                    return; // Skip if no data
                }

                // Create table structure:
                // - First row = headers
                // - Following rows = table data
                w.addTable(s.Columns.Count(), s.Columns.Count * s.Rows.Count);

                // Add header cells (bold font)
                for (int i = 0; i < s.Columns.Count(); i++)
                {
                    w.addTableCell(s.Columns[i], fontWeight: "bold", 0, i);

                }

                // Add table data rows
                for (int i = 0; i < s.Rows.Count; i++)
                {
                    for (int j = 0; j < s.Rows[i].Count; j++)
                    {
                        w.addTableCell(s.Rows[i][j], "normal", i, j);
                    }
                }

            }

        }
    }
}