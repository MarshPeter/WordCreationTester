using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WordCreationTester;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    static async Task Main(string[] args)
    {
        var config = AIConfig.FromEnvironment();

        // TODO: Get the MinimalPayload from the actual API call - this has to wait until we are moving to the actual code file. 

        // This is a test payload, replaces the actual API call for testing. Uncomment this if you want to test with known quantities. 
        var minimalMessage = await TestPayload();

        await GenerateReport(minimalMessage, config);
    }

    // Processor receives message, fetches payload, runs AI, saves result
    private static async Task GenerateReport(ServiceBusRequestMessage minimalMessage, AIConfig config)
    {
        Console.WriteLine("Processor received message.");
        Console.WriteLine($"TenantId = {minimalMessage.TenantId}, AIRequestId = {minimalMessage.AIRequestId}");

        await StatusLogger.LogStatusAsync(minimalMessage.AIRequestId, "Processing", "Message received for processing.");

        using var dbContext = new PayloadDbConnection(config);

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

        // Extract categories
        var categories = new List<string>();
        if (!string.IsNullOrWhiteSpace(requestEntity.ReportCategories))
        {
            categories = JsonSerializer.Deserialize<List<string>>(requestEntity.ReportCategories);
        }

        /*
         * Building the AI message
         * This message provides the AI with a structured prompt that includes all the necessary context for generating a meaningful report.
         * 
         * - Date From / Date To:
         *      Defines the reporting period for AI to only grab data from these dates.
         *      
         * - Search query seed (for retrieval):
         *      Defines the basis for AI retrieval and context, ensuring the report focuses on the statements the user has specified.
         *      
         * - Categories:
         *      Defines the thematic focus of the report (e.g. Medication Safety, Infection Control)
         *      This ensures the AI includes information around the specified categories and draws a clear focus on them.
         *      
         * - Report Type:
         *      Tells the AI what style or format the report should take (e.g. summary, risk analysis, assurance report)
         *      This is essential for aligning output with user's report expectations.
         *      
         * - Index Type:
         *      Defines which index to reference for data collection.
         * 
        */
        string userMessage = $@"
            Using the following information, generate a detailed report.

            Date From: {requestEntity.DateFrom:yyyy-MM-dd}
            Date To: {requestEntity.DateTo:yyyy-MM-dd}

            Search query seed (for retrieval): {string.Join(" ", statements)}

            {(categories != null && categories.Any() ? $"Include information on these categories: {string.Join(", ", categories)}" : "")}


            Generate a report of type: {requestEntity.ReportType}
            
            Index Type: {requestEntity.IndexType}

        ";

        Console.WriteLine(userMessage);

        // Run AI
        await StatusLogger.LogStatusAsync(requestEntity.AIRequestId, "Processing", "AI report generation started.");

        string reportGenerationSystemMessage = """
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
        - You should only ever use straight quotes, no curly quotes. 
        """;

        string reportContent;

        try
        {
            reportContent = await AIRunner.RunAI(config, reportGenerationSystemMessage, userMessage, requestEntity.IndexType);

        }
        catch (Exception ex)
        {
            await StatusLogger.LogStatusAsync(requestEntity.AIRequestId, "Failed", "AI returned no report content.");
            Console.WriteLine(ex.ToString());
            return;
        }

        string jsonGenerationSystemMessage = """
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

            You should only ever use straight quotes, no curly quotes.

            Besides the final JSON, you should output no other information, text or thoughts about the quality of the report.
        """;

        string result;

        try
        {
            result = await AIRunner.RunAI(config, jsonGenerationSystemMessage, reportContent, requestEntity.IndexType, false);
        }
        catch (Exception e)
        {
            Console.WriteLine("JSON translation failed. Raw AI report:");
            Console.WriteLine(reportContent); // Only show the AI report if structured JSON step fails
            Console.WriteLine(e.ToString());
            await StatusLogger.LogStatusAsync(requestEntity.AIRequestId, "Failed", "No structured JSON returned by AI.");
            return;
        }

        try
        {
            await StatusLogger.LogStatusAsync(requestEntity.AIRequestId, "Processing", "Generating Word document and uploading to Blob.");

            string docsDirectory = "./docs";
            Directory.CreateDirectory(docsDirectory);
            ReportCreator.runGeneration(result);

            string filePath = $"{docsDirectory}/Generated.docx";
            string blobName = $"Generated_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
            var blobUrl = await AzureUploader.UploadReportAsync(filePath, blobName, config);

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
        catch (Exception e)
        {
            Console.WriteLine("Word generation/upload failed. Structured JSON:");
            Console.WriteLine(result); // Only show JSON if Word doc generation/upload fails
            Console.WriteLine(e.ToString());
            await StatusLogger.LogStatusAsync(requestEntity.AIRequestId, "Failed", "Word document generation/upload failed.");
            return;
        }
    }

    private static async Task<ServiceBusRequestMessage> TestPayload()
    {

        Console.WriteLine("Simulating Service Bus processor...");

        string userMessage = "Show me a summary of all comments made about medication safety.";

        // Create fake payload
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
                new ReportStatement { Text = userMessage },
                new ReportStatement { Text = "Evaluate medication handling compliance in the last 12 months." },
                new ReportStatement { Text = "Identify gaps in infection control processes across departments." }
            },
            IndexType = "my-index",
        };

        Console.WriteLine($"Created payload with AIRequestId: {payload.AIRequestId}");

        // Save payload to DB + send minimal message (via PayloadProcessor)
        await PayloadProcessor.ProcessPayloadAsync(payload);

        // Simulate receiving the message from Service Bus
        return new ServiceBusRequestMessage
        {
            TenantId = payload.TenantId,
            AIRequestId = payload.AIRequestId
        };
    }
}
