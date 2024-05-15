using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

public class PdfReaderExample
{
    public List<string> ExtractParagraphsFromPdf(string path)
    {
        List<string> paragraphs = new List<string>();
        using (PdfReader reader = new PdfReader(path))
        {
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                string pageContent = PdfTextExtractor.GetTextFromPage(reader, i, strategy);

                // Split the page content by newline characters to get paragraphs
                string[] pageParagraphs = pageContent.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                paragraphs.AddRange(pageParagraphs);
            }
        }
        return paragraphs;
    }
}
To use this method, you would call it with the path to your PDF file:

PdfReaderExample example = new PdfReaderExample();
List<string> paragraphs = example.ExtractParagraphsFromPdf("path/to/your/file.pdf");
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
