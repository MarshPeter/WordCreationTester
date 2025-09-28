using Openize.Words;
using Openize.Words.IElements;

namespace WordCreationTester
{
    public class WordFileGenerator
    {
        private Document _doc;
        private Body _body;
        private Table? _currentTable;
        private bool _currentSectionAlreadyHasRun;

        public WordFileGenerator(Document doc)
        {
            _doc = doc;
            _body = new Body(doc);
            _currentSectionAlreadyHasRun = false;
        }

        public void addTitle(string text, string font = "Times New Roman")
        {
            var p = new Paragraph();

            p.Style = "Title";

            p.AddRun(new Run
            {
                Text = text,
                FontFamily = font
            });

            _body.AppendChild(p);
        }

        public void addHeader(string text)
        {
            addHeader(text, 1);
        }

        public void addHeader(string text, int headingLevel, string font = "Times New Roman")
        {
            var p = new Paragraph();

            p.AddRun(new Run
            {
                Text = text,
                FontFamily = font,
                Color = "Black"
            });

            switch (headingLevel)
            {
                case 1:
                    p.Style = Headings.Heading1;
                    break;
                case 2:
                    p.Style = Headings.Heading2;
                    break;
                case 3:
                    p.Style = Headings.Heading3;
                    break;
                case 4:
                    p.Style = Headings.Heading4;
                    break;
                case 5:
                    p.Style = Headings.Heading5;
                    break;
                case 6:
                    p.Style = Headings.Heading6;
                    break;
                case 7:
                    p.Style = Headings.Heading7;
                    break;
                case 8:
                    p.Style = Headings.Heading8;
                    break;
                case 9:
                    p.Style = Headings.Heading9;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("headingLevel", null, "headingLevel must be between 1 and 9 inclusive");
            }

            _body.AppendChild(p);
            _currentSectionAlreadyHasRun = false;
        }

        public void addParagraph(string text, string font = "Times New Roman")
        {
            var p = new Paragraph();

            // This will add spaces between sentences in sections.
            if (_currentSectionAlreadyHasRun)
            {
                text = " " + text;
            }

            _currentSectionAlreadyHasRun = true;

            p.AddRun(new Run { 
                Text = text,
                FontFamily = font,
            });

            p.Alignment = ParagraphAlignment.Justify;

            _body.AppendChild(p);
        }

        public void addDotpointParagraph(string text, int indentLevel = 1, string font = "Times New Roman")
        {
            var p = new Paragraph();

            p.AddRun(new Run
            {
                Text = text,
                FontFamily = font
            });

            p.Style = "ListParagraph";
            p.IsBullet = true;
            p.NumberingId = 1; // This just needs to be included, number doesn't matter
            p.NumberingLevel = indentLevel;

            _body.AppendChild(p);
        }


        // This is likely to not be used, can maybe be removed in final implementation
        public void addNumericListParagraph(string text, int number, int indentLevel, string font = "Times New Roman")
        {

            var p = new Paragraph();
            p.AddRun(new Run
            {
                Text = text,
                FontFamily = font
            });

            p.Style = "ListParagraph";
            p.NumberingId = 1;
            p.IsAlphabeticNumber = true;
            p.NumberingLevel = indentLevel;

            _body.AppendChild(p);
        }

        // Whenever this is called a new table is created and added to the document
        // we then also remember this table so that we can add cells to it
        // if cells are not added to the table, the table will just be blank.
        public void addTable(int rows, int columns)
        {
            var t = new Table(rows, columns);
            t.Style = _doc.GetElementStyles().TableStyles[1]; // sets solid black borders
            _currentTable = t;
            _body.AppendChild(t);
        }

        // Bolded are seen as table headings, otherwise it's a normal cell
        public void addTableCell(string text, string fontWeight, int rowNum, int colNum, string font = "Times New Roman")
        {
            var row = _currentTable!.Rows[rowNum];
            var cell = row.Cells[colNum];

            var p = new Paragraph();
            p.AddRun(new Run
            {
                Text = text,
                Bold = fontWeight.Equals("bold"),
                FontFamily = font
            });

            p.Alignment = ParagraphAlignment.Center;

            cell.Paragraphs.Add(p);
        }

        public void saveDocument(string dir, string filename)
        {
            _doc.Save($"{dir}/{filename}");
        }
    }
}
