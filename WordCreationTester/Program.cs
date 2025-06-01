using Azure.Identity;
//using MyApp.Search;
using WordCreationTester;
using Azure.Core;
using System.IdentityModel.Tokens.Jwt;


// Uncomment this to work with the report generation files and test them
// ReportCreator.runGeneration(JsonTests.jsonString);

// Uncomment this to work with the AI code. 
// AIRunner.runAI();

// Uncomment this to work with the AIService Creator
// await AIServiceCreator.createSearchResource("swin-testing", "swin-testing-ai-programmatic");

// Uncomment this to work with the AIService Destroyer
// await AIServiceCreator.DeleteSearchService("swin-testing", "swin-testing-ai-programmatic");

// await IndexCreator.CreateSearchResourcesAsync();



// THE FOLLOWING CODE IS FOR INDEXING
string searchServiceName = "swintesting-ai";
string dataSourceName = "my-datasource";
string skillsetName = "my-skillset";
string indexName = "my-index";
string indexerName = "my-indexer";
string blobConnectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING");
string containerName = "documents";
string openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
string openAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");


string dataSourcePayload = $@"
{{
    ""name"": ""{dataSourceName}"",
    ""description"": ""Data source for blob container"",
    ""type"": ""azureblob"",
    ""credentials"": {{ ""connectionString"": ""{blobConnectionString}"" }},
    ""container"": {{ ""name"": ""{containerName}"" }}
}}";

// 1. Create Data Source (as before)
await AzureSearchDataSourceCreator.CreateDataSourceAsync(searchServiceName, dataSourcePayload);

// 3.Create Index
await AzureSearchDataSourceCreator.CreateIndexAsync(searchServiceName, indexName, openAIEndpoint, openAIKey);

// 2.Create Skillset
await AzureSearchDataSourceCreator.CreateSkillsetAsync(searchServiceName, skillsetName, openAIEndpoint, openAIKey, indexName);

// 4.Create Indexer
await AzureSearchDataSourceCreator.CreateIndexerAsync(
    searchServiceName,
   indexerName,
   dataSourceName,
   skillsetName,
   indexName);
