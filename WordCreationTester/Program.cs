using Azure.Core;
using Azure.Identity;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

using System.IO;

using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WordCreationTester;

class Program
{
    static async Task Main(string[] args)
    {
        string userMessage1 = "Show me a summary of all comments made about medication safety.";

        // Create payload
        var payload = new AIReportRequestPayload
        {
            TenantId = Guid.NewGuid(),
            AIRequestId = Guid.NewGuid(),

            CreatedBy = "test.user@example.com",
            DateFrom = new DateTime(2020, 01, 01),
            DateTo = DateTime.UtcNow.Date,

            ReportType = "Compliance Assurance Report",
            ReportCategories = new List<string> { "Medication Safety", "Infection Control" },
            ReportStatements = new List<ReportStatement>
            {
                new ReportStatement { Text = userMessage1 },
                new ReportStatement { Text = "Evaluate medication handling compliance in the last 12 months." },
                new ReportStatement { Text = "Identify gaps in infection control processes across departments." }
            },
            IndexType = "my-index",
        };

        Console.WriteLine($"Created payload with AIRequestId: {payload.AIRequestId}");

        // Save payload to DB + send minimal message (via PayloadProcessor)
        await PayloadProcessor.ProcessPayloadAsync(payload);

        // Simulate receiving the message from Service Bus
        var minimalMessage = new ServiceBusRequestMessage
        {
            TenantId = payload.TenantId,
            AIRequestId = payload.AIRequestId
        };

        Console.WriteLine("Simulating Service Bus processor...");
        await SimulateMessageProcessor(minimalMessage);
    }

    // Processor receives message, fetches payload, runs AI, saves result
    private static async Task SimulateMessageProcessor(ServiceBusRequestMessage minimalMessage)
    {
        Console.WriteLine("Processor received message.");
        Console.WriteLine($"TenantId = {minimalMessage.TenantId}, AIRequestId = {minimalMessage.AIRequestId}");

        await StatusLogger.LogStatusAsync(minimalMessage.AIRequestId, "Processing", "Message received for processing.");

        using var dbContext = new PayloadDbConnection();

        // Fetch full request from DB
        var requestEntity = await dbContext.AIReportRequests
            .FirstOrDefaultAsync(r => r.AIRequestId == minimalMessage.AIRequestId);

        if (requestEntity == null)
        {
            await StatusLogger.LogStatusAsync(minimalMessage.AIRequestId, "Failed", "No payload found in database.");
            Console.WriteLine("No payload found in DB.");
            return;
        }

        // Extract statements
        var statements = new List<string>();
        if (!string.IsNullOrWhiteSpace(requestEntity.ReportStatements))
        {
            statements = JsonSerializer.Deserialize<List<string>>(requestEntity.ReportStatements);
        }

        if (statements == null || statements.Count == 0)
        {
            statements = new List<string> { "Generate a report" };
        }

        //string statementsText = string.Join(Environment.NewLine,
        //    statements.Select((t, i) => $"{i + 1}. {t}")
        //);

        // Extract categories
        var categories = new List<string>();
        if (!string.IsNullOrWhiteSpace(requestEntity.ReportCategories))
        {
            categories = JsonSerializer.Deserialize<List<string>>(requestEntity.ReportCategories);
        }

        // Build AI user message
        string userMessage = $@"
            Using the following information, generate a detailed report.

            Date From: {requestEntity.DateFrom:yyyy-MM-dd}
            Date To: {requestEntity.DateTo:yyyy-MM-dd}

            Search query seed (for retrieval): {string.Join(" ", statements)}

            Include information on these categories: {string.Join(", ", categories ?? new List<string>())}

            Generate a report of type: {requestEntity.ReportType}
            
            Index Type: {requestEntity.IndexType}

        ";

        Console.WriteLine(userMessage);

        // Run AI
        await StatusLogger.LogStatusAsync(requestEntity.AIRequestId, "Processing", "AI report generation started.");

        string systemMessage = """
            You are an AI assistant that helps people find information. Your job is to create reports from the information you find. And only return the report and no other information.
            With this report, you should create a title for the report that is derived from the original messsage. 
            This should mean that your report is in the format of:
            <Title>
            <Content>

        The title should be derived from the original message. 

        The content of your report should be mindful of the following: 
        - The report should be detailed. 
        - Maintain a formal, clear, and professional tone.
        - If you are unsure or cannot find the information, respond with "No data found".
        - Do not include any document tags or additional information.
        """;


        string reportContent = await AIRunner.RunAI(systemMessage, userMessage, requestEntity.IndexType);

        if (string.IsNullOrWhiteSpace(reportContent))
        {
            await StatusLogger.LogStatusAsync(requestEntity.AIRequestId, "Failed", "AI returned no report content.");
            Console.WriteLine("AI returned no report content.");
            return;
        }

        Console.WriteLine("AI Report:");
        Console.WriteLine(reportContent);

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

        string result = await AIRunner.RunAI(systemMessage2, reportContent, requestEntity.IndexType, false);

        if (string.IsNullOrWhiteSpace(result))
        {
            await StatusLogger.LogStatusAsync(requestEntity.AIRequestId, "Failed", "No structured JSON returned by AI.");
            Console.WriteLine("No structured JSON was returned.");
            return;
        }

        Console.WriteLine("Structured JSON:");
        Console.WriteLine(result);

        // Save Word doc + upload to Blob
        await StatusLogger.LogStatusAsync(requestEntity.AIRequestId, "Processing", "Generating Word document and uploading to Blob.");

        string docsDirectory = "./docs";
        Directory.CreateDirectory(docsDirectory);
        ReportCreator.runGeneration(result);

        string filePath = $"{docsDirectory}/Generated.docx";
        string blobName = $"Generated_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
        var blobUrl = await AzureUploader.UploadReportAsync(filePath, blobName);

        // Save results back into DB
        dbContext.AIReportResults.Add(new AIReportResultEntity
        {
            TenantId = requestEntity.TenantId,
            AIRequestId = requestEntity.AIRequestId,
            ReportName = blobName,
            ReportBlobUrl = blobUrl.ToString(),
            Status = "Complete",
            CompletedAt = DateTime.Now
        });

        requestEntity.Status = "Completed";
        await dbContext.SaveChangesAsync();

        await StatusLogger.LogStatusAsync(requestEntity.AIRequestId, "Completed", $"Report generated successfully and uploaded. URL={blobUrl}");

        Console.WriteLine($"Report stored in DB, URL={blobUrl}");
    }
}
