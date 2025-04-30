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
                Report report = JsonConvert.DeserializeObject<Report>(jsonString);

                var doc = new Document();

                var word = new WordFileGenerator(doc);

                word.addTitle(report.ReportTitle);

                foreach (Section s in report.Sections)
                {
                    ParseSection(word, s);
                }

                doc.Save($"{docsDirectory}/{filename}");
            }
            catch (System.Exception ex)
            {
                throw new FileFormatException("An error occurred.", ex);
            }

            static void ParseSection(WordFileGenerator w, Section s)
            {
                if (s.SectionTitle != "")
                {
                    w.addHeader(s.SectionTitle);
                }

                if (s.SectionContext.Equals("paragraph"))
                {
                    parseNormalSection(w, s);
                }
                else if (s.SectionContext.Equals("table"))
                {
                    parseTable(w, s);
                }
                else if (s.SectionContext.Equals("dotpoint"))
                {
                    parseDotPoints(w, s);
                }
                else if (s.SectionContext.Equals("numberList"))
                {
                    parseNumberList(w, s);
                }

                foreach (Section subSection in s.Sections)
                {
                    ParseSection(w, subSection);
                }

            }

        }

        private static void parseNormalSection(WordFileGenerator w, Section s)
        {
            if (s.ParagraphContext.Equals("normal") && s.Content != "")
            {
                w.addParagraph(s.Content);
            }
        }

        private static void parseTable(WordFileGenerator w, Section s)
        {
            if (s.TableData != null)
            {
                w.addTable(s.TableData.RowCount, s.TableData.ColumnCount);

                foreach (Cell c in s.TableData.Cells)
                {

                    w.addTableCell(c.Content, c.FontWeight, c.Row, c.Column);
                }

                if (!s.TableData.Caption.Equals(""))
                {
                    w.addParagraph(s.TableData.Caption);
                }
            }
        }
        private static void parseDotPoints(WordFileGenerator w, Section s, int indent = 1)
        {
            if (s.ParagraphContext.Equals("paragraph"))
            {
                w.addParagraph(s.Content);
            }
            else if (s.ParagraphContext.Equals("dotpoint"))
            {
                w.addDotpointParagraph(s.Content, indent);
            }

            foreach (Section subSection in s.Sections)
            {
                parseDotPoints(w, subSection, indent + 1);
            }
        }

        private static void parseNumberList(WordFileGenerator w, Section s, int indent = 1, int number = 1)
        {
            if (s.ParagraphContext.Equals("paragraph"))
            {
                w.addParagraph(s.Content);
            }
            else if (s.ParagraphContext.Equals("numberList"))
            {
                w.addNumericListParagraph(s.Content, number, indent);
            }

            int subNumber = 1;

            foreach (Section subSection in s.Sections)
            {
                parseNumberList(w, subSection, indent + 1, subNumber++);
            }
        }
    }
}
