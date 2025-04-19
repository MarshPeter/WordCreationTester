using Openize.Words;
using Openize.Words.IElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace WordCreationTester
{
    public class WordFileGenerator
    {
        private Document _doc;
        private Body _body;
        private Table? _currentTable;

        public WordFileGenerator(Document doc)
        {
            _doc = doc;
            _body = new Body(doc);
        }

        public void addTitle(string text)
        {
            var p = new Paragraph();

            p.Style = "Title";

            p.AddRun(new Run
            {
                Text = text
            });

            _body.AppendChild(p);
        }

        public void addHeader(string text)
        {
            addHeader(text, 1);
        }

        public void addHeader(string text, int headingLevel)
        {
            var p = new Paragraph();

            p.AddRun(new Run
            {
                Text = text
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
        }

        public void addParagraph(string text, string font = "Normal")
        {
            var p = new Paragraph();

            p.AddRun(new Run { 
                Text = text,
                FontFamily = font 
            });

            _body.AppendChild(p);
        }

        public void addDotpointParagraph(string text, string font = "Normal", int indentLevel = 1)
        {
            var p = new Paragraph();
            p.IsBullet = true;
            p.NumberingId = 1; // This just needs to be included, number doesn't matter
            p.NumberingLevel = indentLevel;

            p.AddRun(new Run
            {
                Text = text,
                FontFamily = font
            });

            _body.AppendChild(p);
        }

        public void addNumericListParagraph(string text, int number, int indentLevel, string font = "Normal")
        {
            var p = new Paragraph();
            p.IsNumbered = true;
            p.NumberingId = number;
            p.NumberingLevel = indentLevel;
            p.AddRun(new Run
            {
                Text = text,
                FontFamily = font
            });

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
        public void addTableCell(string text, string fontWeight, int rowNum, int colNum)
        {
            var row = _currentTable!.Rows[rowNum];
            var cell = row.Cells[colNum];

            var p = new Paragraph();
            p.AddRun(new Run
            {
                Text = text,
                Bold = fontWeight.Equals("bold")
            });

            cell.Paragraphs.Add(p);
        }

        public void saveDocument(string dir, string filename)
        {
            _doc.Save($"{dir}/{filename}");
        }
    }
}
