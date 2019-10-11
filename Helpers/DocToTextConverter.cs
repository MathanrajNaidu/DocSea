using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Spire.Doc;
using Spire.Pdf;
using Spire.Presentation;
using Spire.Xls;
using Spire.Xls.Core.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace DocSea.Helpers
{
    public static class FileToTextConverter
    {

        public static string Convert(this string path)
        {
           var ext = System.IO.Path.GetExtension(path).ToLower();

            if (ext.Contains("pdf"))
                return path.FromPDFToTextUsingPath();

            else if (ext.Contains("doc"))
                return path.FromDocToTextUsingPath();

            else if (ext.Contains("ppt"))
                return path.FromPPTToTextUsingPath();

            else if (ext.Contains("xls"))
                return path.FromXLSToTextUsingPath();

            else return path.FromAnyFileToTextUsingPath();
        }

        public static string FromPDFToTextUsingPath(this string path)
        {
            using (PdfReader reader = new PdfReader(path))
            {
                StringBuilder text = new StringBuilder();

                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
                }

                return text.ToString();
            }
        }

        public static string FromDocToTextUsingPath(this string path)
        {
            Document document = new Document();
            document.LoadFromFile(path);

            return document.GetText();
        }

        public static string FromPPTToTextUsingPath(this string path)
        {
            Presentation presentation = new Presentation();
            StringBuilder content = new StringBuilder();
            presentation.LoadFromFile(path);

            for (int i = 0; i < presentation.Slides.Count; i++)
            {
                for (int j = 0; j < presentation.Slides[i].Shapes.Count; j++)
                {
                    if (presentation.Slides[i].Shapes[j] is IAutoShape)
                    {
                        IAutoShape shape = presentation.Slides[i].Shapes[j] as IAutoShape;
                        if (shape.TextFrame != null)
                        {
                            foreach (TextParagraph tp in shape.TextFrame.Paragraphs)
                            {
                                content.Append(tp.Text);
                            }
                        }

                    }
                }
            }

            return content.ToString();
        }

        public static string FromXLSToTextUsingPath(this string path)
        {
            Workbook workbook = new Workbook();

            StringBuilder content = new StringBuilder();
            foreach (XlsWorksheet sheet in workbook.Worksheets)
            {
                foreach (var row in sheet.Rows)
                {
                    foreach (var cell in row.Cells)
                    {
                        content.AppendLine(cell.Value);
                    }
                }
            }

            return content.ToString();
        }

        public static string FromAnyFileToTextUsingPath(this string path)
        {
            return File.ReadAllText(path);
        }
    }
}