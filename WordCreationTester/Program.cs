using Azure.Identity;
using WordCreationTester;
using Azure.Core;
using System.IdentityModel.Tokens.Jwt;
using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== AI Report Workflow Start ===");

        string userMessage1 = """
            Show me a summary of all comments made about medication safety.
        """;

        // Create payload
        var payload = new AIReportRequestPayload
        {
            TenantId = Guid.NewGuid(),
            AIRequestId = Guid.NewGuid(),
            CreatedBy = "test.user@example.com",
            DateFrom =  new DateTime(2020, 01, 01),
            DateTo = DateTime.UtcNow.Date,
            Parameters = new ReportParameters
            {
                ReportType = "Summary",
                Filters = new ReportFilters
                {
                    Categories = new List<string> { "Medication Safety" }
                }
            },
            ReportStatements = new List<ReportStatement>
            {
                new ReportStatement { StatementId = "1", Text = userMessage1 }
            },
            IncludeAttachment = false,
            IndexType = "my-index", //sub for real index names - e.g. Assurance, Risk..
        };

        Console.WriteLine($"Created payload with AIRequestId={payload.AIRequestId}");

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

        Console.WriteLine("=== Workflow Complete ===");
    }

    // Processor receives message, fetches payload, runs AI, saves result
    private static async Task SimulateMessageProcessor(ServiceBusRequestMessage minimalMessage)
    {
        Console.WriteLine("Processor received message");
        Console.WriteLine($"TenantId={minimalMessage.TenantId}, AIRequestId={minimalMessage.AIRequestId}");

        using var dbContext = new PayloadDbConnection();

        // Fetch full request from DB
        var requestEntity = await dbContext.AIReportRequests
            .FirstOrDefaultAsync(r => r.AIRequestId == minimalMessage.AIRequestId);

        if (requestEntity == null)
        {
            Console.WriteLine("No payload found in DB.");
            return;
        }

        var fullPayload = JsonSerializer.Deserialize<AIReportRequestPayload>(
            requestEntity.ParametersJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // ensures matching JSON properties
            });

        string json = JsonSerializer.Serialize(fullPayload, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Create reduced payload (only needed properties)
        var aiPayload = new
        {
            DateFrom = fullPayload.DateFrom,
            DateTo = fullPayload.DateTo,
            Parameters = fullPayload.Parameters,
            ReportStatements = fullPayload.ReportStatements,
            AttachmentUrl = fullPayload.AttachmentUrl,
            IndexType = fullPayload.IndexType
        };

        // Collect all statement texts (fallback if none exist)
        var statements = aiPayload.ReportStatements?
            .Select(s => s.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList()
            ?? new List<string> { "Generate a report" };

        // Turn them into a numbered list for readability
        string statementsText = string.Join(Environment.NewLine,
                    statements.Select((t, i) => $"{i + 1}. {t}")
        );

        // Build user message for AI
        string userMessage = $@"
        Using the following information, generate a detailed report.

        Date From: {aiPayload.DateFrom:yyyy-MM-dd}
        Date To: {aiPayload.DateTo:yyyy-MM-dd}

        Report Type: {aiPayload.Parameters?.ReportType}
        Category: {string.Join(", ", aiPayload.Parameters?.Filters?.Categories ?? new List<string>())}
        Attachment URL: {aiPayload.AttachmentUrl}
        Index Type: {aiPayload.IndexType}

        Statements:
        {statementsText}

        Search query seed (for retrieval): {string.Join(" ", statements)}
        ";

        Console.WriteLine(userMessage);

        // Run AI
        string systemMessage = """
            You are an AI assistant that helps people find information. Your job is to create reports from the information you find. And only return the report and no other information.
            With this report, you should create a title for the report that is derived from the original messsage. 
            This should mean that your report is in the format of:
            <Title>
            <Content>

            There should be nothing else in your output message. 
            Your report should be as detailed as possible.
            Do not include Document tags that provide evidence. It doesn't work for us so it is meaningless. 
        """;

        string reportContent = await AIRunner.RunAI(systemMessage, userMessage, aiPayload.IndexType);

        if (string.IsNullOrWhiteSpace(reportContent))
        {
            Console.WriteLine("AI returned no report content");
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

        string result = await AIRunner.RunAI(systemMessage2, reportContent, aiPayload.IndexType, false);

        if (string.IsNullOrWhiteSpace(result))
        {
            Console.WriteLine("No structured JSON was returned.");
            return;
        }

        Console.WriteLine("Structured JSON:");
        Console.WriteLine(result);


        // Save Word doc + upload to Blob
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

        Console.WriteLine($"Report stored in DB, URL={blobUrl}");
    }
}
