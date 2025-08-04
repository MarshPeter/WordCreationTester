using Azure.Identity;
//using MyApp.Search;
using WordCreationTester;
using Azure.Core;

string searchServiceName = "swintesting-ai-programmatic-showcase";
string dataSourceName = "my-datasource";
string skillsetName = "my-skillset";
string indexName = "my-index";
string indexerName = "my-indexer";
string resourceGroup = "TMRRadzen";
string blobConnectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING");
string containerName = "documents";
string openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
string openAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

//Uncomment this to work with the AIService Creator

await AIServiceCreator.createSearchResource(resourceGroup, searchServiceName);

System.Threading.Thread.Sleep(120000);

//await IndexCreator.CreateSearchResourcesAsync();

//THE FOLLOWING CODE IS AN EXAMPLE OF INDEXING CREATION
string dataSourcePayload = $@"
{{
    ""name"": ""{dataSourceName}"",
    ""description"": ""Data source for blob container"",
    ""type"": ""azureblob"",
    ""credentials"": {{ ""connectionString"": ""{blobConnectionString}"" }},
    ""container"": {{ ""name"": ""{containerName}"" }}
}}";

//// 1. Create Data Source (as before)
bool datasourceCreated = await AzureSearchDataSourceCreator.CreateDataSourceAsync(searchServiceName, dataSourcePayload);

if (!datasourceCreated)
{
    Console.WriteLine($@"The datasource {dataSourceName} was not created in {searchServiceName}");
    return;
}

//// 3.Create Index
bool indexCreated = await AzureSearchDataSourceCreator.CreateIndexAsync(searchServiceName, indexName, openAIEndpoint, openAIKey);

if (!indexCreated)
{
    Console.WriteLine($@"The index {dataSourceName} was not created in {searchServiceName}");
    return;
}

//// 2.Create Skillset
bool skillsetCreated = await AzureSearchDataSourceCreator.CreateSkillsetAsync(searchServiceName, skillsetName, openAIEndpoint, openAIKey, indexName);

if (!skillsetCreated)
{
    Console.WriteLine($@"The skillset {dataSourceName} was not created in {searchServiceName}");
    return;
}


//// 4.Create Indexer
bool indexerCreated = await AzureSearchDataSourceCreator.CreateIndexerAsync(
    searchServiceName,
   indexerName,
   dataSourceName,
   skillsetName,
   indexName);

if (!skillsetCreated)
{
    Console.WriteLine($@"The indexer {dataSourceName} was not created in {searchServiceName}");
    return;
}