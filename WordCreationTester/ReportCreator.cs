using Newtonsoft.Json;
using Openize.Words;

namespace WordCreationTester
{
    public static class ReportCreator
    {
        public static void runGeneration(string jsonString)
        {
            string docsDirectory = "./docs";
            string filename = "Generated.docx";

            try
            {
                // Deserialize the JSON string into a Report object
                List<ReportSegment> report = JsonConvert.DeserializeObject<List<ReportSegment>>(jsonString);

                var doc = new Document();

                var word = new WordFileGenerator(doc);

                foreach (ReportSegment s in report)
                {
                    ParseSection(word, s);
                }

                doc.Save($"{docsDirectory}/{filename}");
            }
            catch (System.Exception ex)
            {
                throw new FileFormatException("An error occurred.", ex);
            }

        }
        static void ParseSection(WordFileGenerator w, ReportSegment s)
        {

            if (s.Type.Equals("title"))
            {
                if (s.Text == null || s.Text.Equals("")) return;

                w.addTitle(s.Text);
            }

            if (s.Type.Equals("header"))
            {
                if (s.Text == null || s.Text.Equals("")) return;

                w.addHeader(s.Text);
            }

            if (s.Type.Equals("paragraph"))
            {
                if (s.Text == null || s.Text.Equals("")) return;

                w.addParagraph(s.Text);
            }

            if (s.Type.Equals("list"))
            {
                if (s.Items == null || s.Items.Count == 0) return;

                foreach (string point in s.Items)
                {
                    w.addDotpointParagraph(point);
                }
            }

            if (s.Type.Equals("table"))
            {
                int nonHeaderCells = 0;
                if (s.Columns == null || s.Columns.Count == 0 ||
                    s.Rows == null || s.Rows.Count == 0)
                {
                    return;
                }

                w.addTable(s.Columns.Count(), s.Columns.Count * s.Rows.Count);

                for (int i = 0; i < s.Columns.Count(); i++)
                {
                    w.addTableCell(s.Columns[i], fontWeight: "bold", 0, i);

                }

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