using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WordCreationTester.Azure;
using WordCreationTester.Configuration;
using WordCreationTester.DTO;
using WordCreationTester.Services;

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

        await PayloadOutcomeUpdater.UpdatePayloadStatus(minimalMessage.AIRequestId, 1, "Message received for processing.");

        using var dbContext = new PayloadDbConnection(config);

        // Fetch full request from DB
        var requestEntity = await dbContext.AIReportRequest
            .FirstOrDefaultAsync(r => r.Id == minimalMessage.AIRequestId);

        if (requestEntity == null)
        {
            await PayloadOutcomeUpdater.UpdatePayloadStatus(minimalMessage.AIRequestId, 1, "No payload found in database.");
            Console.WriteLine("No payload found in DB.");
            return;
        }

        // Extract statements
        var statements = new List<string>();
        if (!string.IsNullOrWhiteSpace(requestEntity.ReportParametersJSON))
        {
            statements = JsonSerializer.Deserialize<List<string>>(requestEntity.ReportParametersJSON);
        }

        if (statements == null || statements.Count == 0)
        {
            requestEntity.Status = 1;
            await dbContext.SaveChangesAsync();

            return;
        }

        // Retrieve index
        var index = await dbContext.AIReportIndexes
            .FirstOrDefaultAsync(r => r.Id == requestEntity.AIReportIndexId && r.CreatedById == requestEntity.CreatedById.ToString());

        if (index == null)
        {
            await PayloadOutcomeUpdater.UpdatePayloadStatus(minimalMessage.AIRequestId, 1, "Index was not found attached to request");
            return;
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
         * - Report Type:
         *      Tells the AI what style or format the report should take (e.g. summary, risk analysis, assurance report)
         *      This is essential for aligning output with user's report expectations.
         *      
         * - Index Type:
         *      Defines which index to reference for data collection.
         * 
        */
        string userMessage = $@"
            Using the following search request, generate a detailed report.

            Search Request: {string.Join(" ", statements)}

            Date From: {requestEntity.FromDt:yyyy-MM-dd}
            Date To: {requestEntity.ToDt:yyyy-MM-dd}

            Generate a report of type: Compliance Assurance Report
            ";


        // Run AI
        await PayloadOutcomeUpdater.UpdatePayloadStatus(requestEntity.Id, 1, "AI report generation started.");

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
            // Run the AI to generate the initial raw report content
            reportContent = await AIRunner.RunAI(config, reportGenerationSystemMessage, userMessage, "my-index", outputFormat: OutputFormat.PlainText);

        }
        catch (Exception ex)
        {
            Console.WriteLine("AI report generation failed.");
            Console.WriteLine("User message was:");
            Console.WriteLine(userMessage);
            await PayloadOutcomeUpdater.UpdatePayloadStatus(requestEntity.Id, 2, "AI returned no report content.");
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

        string structuredJsonReport;

        try
        {
            // Convert the AI report into a structured JSON format for further processing
            structuredJsonReport = await AIRunner.RunAI(config, jsonGenerationSystemMessage, reportContent, index.IndexName, false, outputFormat: OutputFormat.Json);
        }
        catch (Exception e)
        {
            Console.WriteLine("JSON translation failed. Raw AI report:");
            Console.WriteLine(reportContent); // Only show the AI report if structured JSON step fails
            Console.WriteLine("User message was:");
            Console.WriteLine(userMessage);
            Console.WriteLine(e.ToString());
            await PayloadOutcomeUpdater.UpdatePayloadStatus(requestEntity.Id, 1, "No structured JSON returned by AI.");
            return;
        }

        try
        {

            // Generate the Word document from JSON and upload it to Azure Blob Storage
            await PayloadOutcomeUpdater.UpdatePayloadStatus(requestEntity.Id, 1, "Generating Word document and uploading to Blob.");

            string docsDirectory = "./docs";
            Directory.CreateDirectory(docsDirectory);
            ReportCreator.RunGeneration(structuredJsonReport);

            string filePath = $"{docsDirectory}/Generated.docx";
            string blobName = $"Generated_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
            var blobUrl = await AzureUploader.UploadReportAsync(filePath, blobName, config);

            // TODO: Setup Attachments
            await PayloadOutcomeUpdater.UpdatePayloadStatus(requestEntity.Id, 2, "Report successfull updated");

            await dbContext.SaveChangesAsync();

            await PayloadOutcomeUpdater.UpdatePayloadStatus(requestEntity.Id, 1, $"Report generated successfully and uploaded. URL={blobUrl}");

            Console.WriteLine($"Report stored in DB, URL={blobUrl}");
        }
        catch (Exception e)
        {
            Console.WriteLine("Word generation/upload failed. Structured JSON:");
            Console.WriteLine(structuredJsonReport); // Only show JSON if Word doc generation/upload fails
            Console.WriteLine("User message was:");
            Console.WriteLine(userMessage);
            Console.WriteLine(e.ToString());
            await PayloadOutcomeUpdater.UpdatePayloadStatus(requestEntity.Id, 1, "Word document generation/upload failed.");
            return;
        }
    }

    private static async Task<ServiceBusRequestMessage> TestPayload()
    {

        Console.WriteLine("Simulating Service Bus processor...");

        string userMessage = "Show me a summary of all comments made about medication safety. Include Medication Safety and Infection Control information";

        // Create fake payload
        var payload = new AIReportRequest
        {
            Id = Guid.NewGuid(),
            DisplayId = "1",
            ReportType = 1,
            FromDt = new DateTime(2020, 01, 01),
            ToDt = DateTime.UtcNow.Date,
            ReportParametersJSON = JsonSerializer.Serialize(new[] { userMessage }),
            CreatedById = Environment.GetEnvironmentVariable("FAKE_OWNER") ??
                throw new InvalidOperationException("FAKE_OWNER environment variable is not set."),
            CreatedDt = DateTime.Now,
            Status = 1,   
            AIReportIndexId = new Guid("115e86e0-f88f-42d5-ab71-3830f335c2b5"),
        };

        Console.WriteLine($"Created payload with AIRequestId: {payload.Id}");

        // Save payload to DB + send minimal message (via PayloadProcessor)
        await PayloadProcessor.ProcessPayloadAsync(payload);

        // Simulate receiving the message from Service Bus
        return new ServiceBusRequestMessage
        {
            TenantId = new Guid(payload.CreatedById),
            AIRequestId = payload.Id
        };
    }
}
