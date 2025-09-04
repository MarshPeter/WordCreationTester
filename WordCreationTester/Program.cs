using Azure.Core;
using Azure.Identity;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Threading.Tasks;
using WordCreationTester;

class Program
{
    static async Task Main(string[] args)
    {
        string userMessage1 = """
         List the most common medication errors reported in the last month.
        """;

        string systemMessage1 = """
        You are an AI assistant that helps people find information and create reports. 
        Only return the report in this format:

        <Title>
        <Content>

        The title should be derived from the original message. 
        The report should be detailed. 
        If you are unsure or cannot find the information, respond with "No data found".
        Do not include any document tags or additional information.
        """;



        // Add core instructions to system message
        var combinedSystemMessage = @"
You are a structured AI assistant.
- Always respond in valid JSON format.
- Maintain a formal, clear, and professional tone.
- Follow a consistent structure in all responses.
- If there is no relevant information, respond exactly with 'No data found'.

" + systemMessage1;





        string userMessage2 = await AIRunner.RunAI(combinedSystemMessage, userMessage1);



        if (userMessage2 == null)
        {
            Console.WriteLine("No data was obtained");
            return;
        }

    
        try
        {
            JsonDocument.Parse(userMessage2.Trim());
        }
        catch
        {
            userMessage2 = "\"No data found\""; // fallback if AI output is invalid JSON
        }

        Console.WriteLine("AI Report:");
        Console.WriteLine(userMessage2);

        string systemMessage2 = """
            Your sole duty is to estimate from these reports what are headers, paragraphs and dot points and to return the report in a array JSON Format that follows this layout exactly:

            [
              {
                "type": "header" | "paragraph" | "list" | "table" | "title",
                "text?": string,
                "items?": [string],
                "columns?": [string],
                "rows?": [[string]]
              }
            ]

            To give some context on each json key:

            type is the type of each portion of the report, if you think it would be a paragraph, the type should be a "paragraph" for example. A type of "title" should only appear once if at all and should either be the first item in the list or none at all. It is ok for there to be no title, that is not unusual, so do not force anything to be a title if it does not sound like one. 

            Text is simply the text of the header or paragraph, a table will not have a text row. 

            Items are only present in a list, it contains all separate dot points that the list contains. 

            Columns are the column headers of a table. 

            Rows are all the data present in the table. Where every data point is comma separated and every row is in its own array within the rows array. 

            Besides the final JSON, you should output no other information, text or thoughts about the quality of the report.
        """;

        string result = await AIRunner.RunAI(systemMessage2, userMessage2, false);

        if (string.IsNullOrWhiteSpace(result))
        {
            Console.WriteLine("No structured JSON was returned.");
            return;
        }

        Console.WriteLine("Structured JSON:");
        Console.WriteLine(result);


        // Ensure the docs directory exists
        string docsDirectory = "./docs";
        if (!Directory.Exists(docsDirectory))
        {
            Directory.CreateDirectory(docsDirectory);
        }


        // Generate the Word document
        ReportCreator.runGeneration(result);

        // Upload to Azure Blob Storage
        string filePath = $"{docsDirectory}/Generated.docx";
        string blobName = $"Generated_{DateTime.Now:yyyyMMdd_HHmmss}.docx";

        try
        {
            await AzureUploader.UploadReportAsync(filePath, blobName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Upload failed: {ex.Message}");
        }
    }
}
