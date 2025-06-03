using Azure.Identity;
//using MyApp.Search;
using WordCreationTester;
using Azure.Core;
using System.IdentityModel.Tokens.Jwt;


// Uncomment this to work with the report generation files and test them
// ReportCreator.runGeneration(JsonTests.jsonString);

// Uncomment this to work with the AI code. 

string userMessage1 = """
                    Show me a summary of all comments made about medication safety.
                                        
                    """;
string systemMessage1 = """
                    You are an AI assistant that helps people find information. Your job is to create reports from the information you find. And only return the report and no other information.
                    With this report, you should create a title for the report that is derived from the original messsage. 
                    This should mean that your report is in the format of:
                    <Title>
                    <Content>

                    There should be nothing else in your output message. 
                    Your report should be as detailed as possible.
                    Do not include Document tags that provide evidence. It doesn't work for us so it is meaningless. 
                    """;

string userMessage2 = await AIRunner.RunAI(systemMessage1, userMessage1);

if (userMessage2 == null)
{
    Console.WriteLine("No data was obtained");
    return;
}

Console.WriteLine(userMessage2);

// This is just if you are suspicious and think that something isn't working with the JSON retrieval.
//string userMessage2 = """
//        Medication Safety Comments Summary

//    1. **VTE Risk Assessment**: There was a comment indicating a lack of evidence regarding the VTE risk assessment on the NIMC (Medication Chart). This was noted in multiple audits, highlighting concerns about medication safety standards [doc1][doc2][doc5].

//    2. **Regular Medications**: It was confirmed that the patient does take regular medications, which is a key point in ensuring medication safety and proper management processes [doc1].

//    3. **Medication List on Discharge**: A comment noted that there was no evidence in the progress notes or medication management plans (MMP/NMMP) that a medication list was provided to the patient upon discharge. This raises concerns regarding the continuity of care and patient safety [doc2].

//    4. **Pharmacy Review**: There were comments indicating that pharmacy reviews were not conducted for patients identified in the RED or AMBER range for medication risk factors. This is critical for ensuring patient safety and effective medication management [doc3][doc4].

//    5. **General Comments on Medication Processes**: Comments also reflected on general medication safety processes and management, emphasizing the importance of adhering to the National Safety Quality Health Standards [doc3][doc4].

//    These comments collectively underscore the necessity for rigorous medication safety practices in clinical settings to protect patient health and ensure compliance with established standards.

//    """;

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

string result = await AIRunner.RunAI(systemMessage2, userMessage1, false);

Console.WriteLine(result);

// Uncomment this to work with the AIService Creator
// await AIServiceCreator.createSearchResource("swin-testing", "swin-testing-ai-programmatic");

// Uncomment this to work with the AIService Destroyer
// await AIServiceCreator.DeleteSearchService("swin-testing", "swin-testing-ai-programmatic");

// await IndexCreator.CreateSearchResourcesAsync();

// THE FOLLOWING CODE IS AN EXAMPLE OF INDEXING CREATION
//string searchServiceName = "swintesting-ai";
//string dataSourceName = "my-datasource";
//string skillsetName = "my-skillset";
//string indexName = "my-index";
//string indexerName = "my-indexer";
//string blobConnectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING");
//string containerName = "documents";
//string openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
//string openAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");


//string dataSourcePayload = $@"
//{{
//    ""name"": ""{dataSourceName}"",
//    ""description"": ""Data source for blob container"",
//    ""type"": ""azureblob"",
//    ""credentials"": {{ ""connectionString"": ""{blobConnectionString}"" }},
//    ""container"": {{ ""name"": ""{containerName}"" }}
//}}";

//// 1. Create Data Source (as before)
//await AzureSearchDataSourceCreator.CreateDataSourceAsync(searchServiceName, dataSourcePayload);

//// 3.Create Index
//await AzureSearchDataSourceCreator.CreateIndexAsync(searchServiceName, indexName, openAIEndpoint, openAIKey);

//// 2.Create Skillset
//await AzureSearchDataSourceCreator.CreateSkillsetAsync(searchServiceName, skillsetName, openAIEndpoint, openAIKey, indexName);

//// 4.Create Indexer
//await AzureSearchDataSourceCreator.CreateIndexerAsync(
//    searchServiceName,
//   indexerName,
//   dataSourceName,
//   skillsetName,
//   indexName);
