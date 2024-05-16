using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ImageToText;
using Microsoft.SemanticKernel.Planning;
using RAGTesting;
using System.Globalization;
using System.Linq;
#pragma warning disable SKEXP0060
#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0001
public class Program()
{

    public static async Task Main(string[] args)
    {
        await RAG();
    }
    public static async Task RAG()
    {
        IKernelBuilder builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(
                settings.OpenAIDeploymentName,
                            settings.OpenAIEndpoint,
                                    settings.OpenAIAPIKey);
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        Kernel kernel = builder.Build();

        builder.Services.AddSingleton(loggerFactory);
        await RAGAction(kernel);
    }
    public static async Task RAGAction(Kernel kernel)
    {
        kernel.ImportPluginFromType<SearchPlugin>("Searcher");
        FunctionCallingStepwisePlanner planner = new FunctionCallingStepwisePlanner(new FunctionCallingStepwisePlannerOptions() { });
        FunctionCallingStepwisePlannerResult result = await planner.ExecuteAsync(kernel, "If I ask a question and provide the path for a pdf, search the pdf and take the results, plug it back into a new prompt with my initial question, and give me the result.  The final output should be the response to a prompt which contains my original prompt along with the informations you gathered from the pdf.  As in, answer the user's original question using the information from the pdf (the new prompt).");
        foreach (ChatMessageContent step in result.ChatHistory.ToList<ChatMessageContent>())
        {
            Console.WriteLine(step);
            Console.WriteLine();
        }
    }
}

public class PdfReaderExample
{
    public List<string> ExtractParagraphsFromPdf(string pdfpath)
    {
        var codePages = CodePagesEncodingProvider.Instance;
        Encoding.RegisterProvider(codePages);
        List<string> paragraphs = new List<string>();
        using (PdfReader reader = new PdfReader(pdfpath))
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
public class SearchPlugin()
{
    [KernelFunction, Description("Finds related information from pdf document and returns a list of strings that are relevant.")]
    public static List<string> SearchInPdf([Description("Path to pdf with valuable information.")] string pdfPath, [Description("Query to search in pdf.")] string query)
    {
        PdfReaderExample example = new PdfReaderExample();
        List<string> paragraphs = example.ExtractParagraphsFromPdf(pdfPath);
        return SearchInList(paragraphs, query);
    }
    private static List<string> SearchInList(List<string> list, string query)
    {
        // Convert the query to lowercase for case-insensitive search
        string lowerCaseQuery = query.ToLower();

        // Calculate overlap for each string in the list and store it along with the string
        var overlapList = list.Select(s => new
        {
            String = s,
            Overlap = CalculateOverlap(s.ToLower(), lowerCaseQuery)
        });

        // Sort the list by overlap in descending order and take the top 3
        var topMatches = overlapList.OrderByDescending(x => x.Overlap).Take(3).Select(x => x.String).ToList();

        return topMatches;
    }
    [KernelFunction, Description("Gets user question, returned string is the question to be asked")]
    public static string GetUserQuestion()
    {
        Console.WriteLine("Enter your question:");
        return Console.ReadLine();
    }
    [KernelFunction, Description("Generate new question for chatgpt. Returns the string that should be the final prompt")]
    public static string GenerateNewQuestion([Description("Original question asked by user.")] string question, [Description("List of relevant information from pdf.")] List<string> pdfResults)
    {
        // Combine the original question and pdf results to generate a new question
        StringBuilder newQuestion = new StringBuilder();
        newQuestion.Append(question);
        newQuestion.Append(" ");
        newQuestion.Append(string.Join(" ", pdfResults));
        return newQuestion.ToString();
    }


    // Helper method to calculate the overlap between two strings
    private static int CalculateOverlap(string str, string query)
    {
        int overlap = 0;
        foreach (var word in query.Split(' '))
        {
            if (str.Contains(word))
            {
                overlap += word.Length;
            }
        }
        return overlap;
    }

}
