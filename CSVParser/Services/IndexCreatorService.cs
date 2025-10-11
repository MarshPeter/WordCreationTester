using Azure.Core;
using Azure.Identity;
using CsvParser.Configuration;
using CsvParser.Data.Models;
using CsvParser.DTO;
using CSVParser.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;


namespace CsvParser.Services
{
    public class IndexCreatorService
    {
        private readonly TMRRadzenContext _dbContext;
        private readonly ILogger<IndexCreatorService> _logger;
        private readonly AIConfig _settings;

        // Creates and manages Azure Cognitive Search indexes, data sources, skillsets, and indexers
        public IndexCreatorService(
            TMRRadzenContext dbContext,
            ILogger<IndexCreatorService> logger,
            IOptions<AIConfig> settings)
        {
            _dbContext = dbContext;
            _logger = logger;
            _settings = settings.Value;
        }

        // Creates a complete Azure Search index with data source, skillset, and indexer
        public async Task<bool> CreateIndex(string indexName)
        {
            // Azure Cognitive Search service and related configuration
            string searchServiceName = $"{_settings.TenantId}-ai-search-reports";
            string dataSourceName = $"{indexName}-data-source";
            string skillsetName = $"{indexName}-skillset";
            string indexerName = $"{indexName}-indexer";

            string blobConnectionString = _settings.BlobConnectionString;
            string containerName = _settings.BlobContainerBaseName;

            // Azure OpenAI configuration
            string openAIEndpoint = _settings.LLMAIEndpoint;
            string openAIKey = _settings.LLMAIKey;

            // Data source payload - points to the folder in blob storage
            // The indexer will process ALL CSV files in this folder (handles multiple files automatically!)
            string dataSourcePayload = $@"
{{
    ""name"": ""{dataSourceName}"",
    ""description"": ""Data source for blob container"",
    ""type"": ""azureblob"",
    ""credentials"": {{ ""connectionString"": ""{blobConnectionString}"" }},
    ""container"": {{ ""name"": ""{containerName}"", ""query"": ""{indexName}/"" }}
}}";

            // 1. Create Data Source
            bool datasourceCreated = await CreateDataSourceAsync(searchServiceName, dataSourcePayload);

            if (!datasourceCreated)
            {
                _logger.LogError("The datasource {DataSourceName} was not created in {SearchServiceName}",
                    dataSourceName, searchServiceName);
                return false;
            }

            // 2. Create Index
            bool indexCreated = await CreateIndexAsync(searchServiceName, indexName, openAIEndpoint, openAIKey);

            if (!indexCreated)
            {
                _logger.LogError("The index {IndexName} was not created in {SearchServiceName}",
                    indexName, searchServiceName);

                // Clean up already created resources
                await DeleteDataSourceAsync(searchServiceName, dataSourceName);
                return false;
            }

            // 3. Create Skillset
            bool skillsetCreated = await CreateSkillsetAsync(searchServiceName, skillsetName, openAIEndpoint, openAIKey, indexName);

            if (!skillsetCreated)
            {
                _logger.LogError("The skillset {SkillsetName} was not created in {SearchServiceName}",
                    skillsetName, searchServiceName);

                // Clean up already created resources
                await DeleteIndexAsync(searchServiceName, indexName);
                await DeleteDataSourceAsync(searchServiceName, dataSourceName);
                return false;
            }

            // 4. Create Indexer
            bool indexerCreated = await CreateIndexerAsync(
               searchServiceName,
               indexerName,
               dataSourceName,
               skillsetName,
               indexName);

            if (!indexerCreated)
            {
                _logger.LogError("The indexer {IndexerName} was not created in {SearchServiceName}",
                    indexerName, searchServiceName);

                // Clean up already created resources
                await DeleteIndexAsync(searchServiceName, indexName);
                await DeleteDataSourceAsync(searchServiceName, dataSourceName);
                await DeleteSkillsetAsync(searchServiceName, skillsetName);
                return false;
            }

            _logger.LogInformation("Successfully created complete search index: {IndexName}", indexName);
            return true;
        }

        // Updates or creates database records for Azure Search indexes
        public async Task<bool> UpdateDatabaseIndexInformation(
            List<IndexDefinition> createdIndexes,
            string tenantId,
            CancellationToken ct = default)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            var newlyCreatedIndexes = new List<string>();

            try
            {
                // Get name of the indexes being created 
                var indexNames = createdIndexes.Select(x => x.IndexName).ToList();

                // Get the owner ID from environment
                // Get existing AIReportIndexes that match the provided index names for this particular tenant
                string ownerId = Environment.GetEnvironmentVariable("FAKE_OWNER")
                    ?? throw new InvalidOperationException("FAKE_OWNER environment variable not set");

                // Get existing AIReportIndexes that match the provided index names
                var existingReportIndexes = await _dbContext.AIReportIndexes
                    .Where(r => indexNames.Contains(r.IndexName) && r.CreatedById == ownerId)
                    .ToListAsync(ct);

                // Create a dictionary for quick lookup of existing report indexes by name
                var reportIndexMap = existingReportIndexes.ToDictionary(r => r.IndexName, r => r);

                // Prepare lists for any new docs and tenant–index links we might need to create
                var now = DateTime.UtcNow;

                // Go through each index we want to create
                foreach (var indexDef in createdIndexes)
                {
                    // --- AIReportIndexes ---
                    AIReportIndexes? reportIndex;
                    bool newIndex = false;
                    if (!reportIndexMap.TryGetValue(indexDef.IndexName, out reportIndex))
                    {
                        // Create new AIReportIndex
                        reportIndex = new AIReportIndexes
                        {
                            Id = Guid.NewGuid(),
                            DisplayId = "1",
                            IndexName = indexDef.IndexName,
                            IndexDescription = indexDef.IndexDescription,
                            UIDisplayName = indexDef.DisplayName,
                            IndexLastUpdatedDt = DateTime.UtcNow,
                            CreatedById = Environment.GetEnvironmentVariable("FAKE_OWNER"),
                            CreatedDt = DateTime.UtcNow,
                            Status = "In Progress: Creating Index"
                        };
                        _dbContext.AIReportIndexes.Add(reportIndex);
                        reportIndexMap[indexDef.IndexName] = reportIndex;

                        newlyCreatedIndexes.Add(indexDef.IndexName);
                        newIndex = true;
                    }
                    else
                    {
                        // Update existing index
                        if (reportIndex.IndexDescription != indexDef.IndexDescription)
                        {
                            reportIndex.IndexDescription = indexDef.IndexDescription;
                        }

                        if (reportIndex.UIDisplayName != indexDef.DisplayName)
                        {
                            reportIndex.UIDisplayName = indexDef.DisplayName;
                        }

                        // Update timestamp to indicate CSV documents have been refreshed
                        reportIndex.IndexLastUpdatedDt = now;

                        // Update status
                        reportIndex.Status = "Updated: CSV data refreshed";
                    }

                    // Only create Azure Search index if this is a new index
                    if (newIndex)
                    {
                        _logger.LogInformation("Creating Azure Search index for: {IndexName}", indexDef.IndexName);

                        try
                        {
                            bool res = await CreateIndex(indexDef.IndexName);

                            if (res)
                            {
                                reportIndex.Status = "Success: Index created";
                                _logger.LogInformation("Successfully created Azure Search index: {IndexName}", indexDef.IndexName);
                            }
                            else
                            {
                                reportIndex.Status = "Failed: Index creation failed";
                                _logger.LogError("Failed to create Azure Search index: {IndexName}", indexDef.IndexName);
                            }
                        }
                        catch (Exception ex)
                        {
                            reportIndex.Status = $"Error: {ex.Message}";
                            _logger.LogError(ex, "Exception while creating Azure Search index: {IndexName}", indexDef.IndexName);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Index already exists, CSV data refreshed: {IndexName}", indexDef.IndexName);
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation(
                    "Updated index information for tenant {TenantId}. Newly created indexes: {Indexes}",
                    tenantId,
                    string.Join(", ", newlyCreatedIndexes.Any() ? newlyCreatedIndexes : new[] { "None (existing indexes updated)" }));

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error updating index information for tenant {TenantId}", tenantId);
                return false;
            }
        }

        // Creates an Azure Search data source pointing to blob storage
        public static async Task<bool> CreateDataSourceAsync(
            string searchServiceName,
            string dataSourcePayload)
        {
            // The endpoint for the data source API
            string endpoint = $"https://{searchServiceName}.search.windows.net/datasources?api-version=2024-07-01";

            // Acquire a token using DefaultAzureCredential
            var credential = new DefaultAzureCredential();
            var tokenRequestContext = new TokenRequestContext(
                new[] { "https://search.azure.com/.default" });
            AccessToken token = await credential.GetTokenAsync(tokenRequestContext);

            using var httpClient = new HttpClient();

            // Add auth header and send data source payload to Azure Search
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token.Token);


            var content = new StringContent(dataSourcePayload, Encoding.UTF8, "application/json");


            var response = await httpClient.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Data source created successfully.");
                return true;
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to create data source: {response.Content}");
                Console.WriteLine(error);

                return false;
            }
        }
        // CreateIndexerAsync creates an Azure Cognitive Search indexer to pull data from a data source
        public static async Task<bool> CreateSkillsetAsync(
            string searchServiceName,
            string skillsetName,
            string openAIEndpoint,
            string openAIKey,
            string indexName)
        {
            string endpoint = $"https://{searchServiceName}.search.windows.net/skillsets/{skillsetName}?api-version=2024-07-01";


            // JSON payload for skillset: splits documents into chunks, generates embeddings with OpenAI, 
            // and projects results (chunk, title, vector) into the target index
            string skillsetPayload = $@"
{{
  ""name"": ""{skillsetName}"",
  ""description"": ""Skillset to chunk documents and generate embeddings"",
  ""skills"": [
    {{
      ""@odata.type"": ""#Microsoft.Skills.Text.SplitSkill"",
      ""name"": ""#1"",
      ""description"": ""Split skill to chunk documents"",
      ""context"": ""/document"",
      ""defaultLanguageCode"": ""en"",
      ""textSplitMode"": ""pages"",
      ""maximumPageLength"": 2000,
      ""pageOverlapLength"": 500,
      ""maximumPagesToTake"": 0,
      ""inputs"": [
        {{
          ""name"": ""text"",
          ""source"": ""/document/content"",
          ""inputs"": []
        }}
      ],
      ""outputs"": [
        {{
          ""name"": ""textItems"",
          ""targetName"": ""pages""
        }}
      ]
    }},
    {{
      ""@odata.type"": ""#Microsoft.Skills.Text.AzureOpenAIEmbeddingSkill"",
      ""name"": ""#2"",
      ""context"": ""/document/pages/*"",
      ""resourceUri"": ""{openAIEndpoint}"",
      ""apiKey"": ""{openAIKey}"",
      ""deploymentId"": ""text-embedding-ada-002"",
      ""dimensions"": 1536,
      ""modelName"": ""text-embedding-ada-002"",
      ""inputs"": [
        {{
          ""name"": ""text"",
          ""source"": ""/document/pages/*"",
          ""inputs"": []
        }}
      ],
      ""outputs"": [
        {{
          ""name"": ""embedding"",
          ""targetName"": ""text_vector""
        }}
      ]
    }}
  ],
  ""indexProjections"": {{
    ""selectors"": [
      {{
        ""targetIndexName"": ""{indexName}"",
        ""parentKeyFieldName"": ""parent_id"",
        ""sourceContext"": ""/document/pages/*"",
        ""mappings"": [
          {{
            ""name"": ""text_vector"",
            ""source"": ""/document/pages/*/text_vector"",
            ""inputs"": []
          }},
          {{
            ""name"": ""chunk"",
            ""source"": ""/document/pages/*"",
            ""inputs"": []
          }},
          {{
            ""name"": ""title"",
            ""source"": ""/document/title"",
            ""inputs"": []
          }}
        ]
      }}
    ],
    ""parameters"": {{
      ""projectionMode"": ""skipIndexingParentDocuments""
    }}
  }}
}}
";
            // Create HTTP client and send skillset payload to Azure Search
            using var client = await AzureSearchHttpClientFactory.CreateAsync();
            var content = new StringContent(skillsetPayload, Encoding.UTF8, "application/json");

            var response = await client.PutAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Skillset created successfully.");
                return true;
            }
            else
            {
                Console.WriteLine($"Skillset creation failed: {await response.Content.ReadAsStringAsync()}");
                return false;
            }
        }
        // Create a new Azure Search index with text fields, vector search, and semantic config
        public static async Task<bool> CreateIndexAsync(
            string searchServiceName,
            string indexName,
            string openAIEndpoint,
            string openAIKey)
        {
            string endpoint = $"https://{searchServiceName}.search.windows.net/indexes/{indexName}?api-version=2024-07-01";
            var algorithmName = $"{indexName}-index-algorithm";
            var profileName = $"{indexName}-azureOpenAi-text-profile";
            var vectorizerName = $"{indexName}-azureOpenAi-text-vectorizer";
            var semanticName = $"{indexName}-semantic-configuration";
            // Some of this will need to be updated to be more general eventually. It has a bit too much hard coding atm. 
            // Define index schema and configuration (fields, semantic settings, vector search with OpenAI)
            string indexPayload = $@"
{{
  ""name"": ""{indexName}"",
  ""fields"": [
    {{
      ""name"": ""chunk_id"",
      ""type"": ""Edm.String"",
      ""searchable"": true,
      ""filterable"": false,
      ""retrievable"": true,
      ""stored"": true,
      ""sortable"": true,
      ""facetable"": false,
      ""key"": true,
      ""analyzer"": ""keyword"",
      ""synonymMaps"": []
    }},
    {{
      ""name"": ""parent_id"",
      ""type"": ""Edm.String"",
      ""searchable"": false,
      ""filterable"": true,
      ""retrievable"": true,
      ""stored"": true,
      ""sortable"": false,
      ""facetable"": false,
      ""key"": false,
      ""synonymMaps"": []
    }},
    {{
      ""name"": ""chunk"",
      ""type"": ""Edm.String"",
      ""searchable"": true,
      ""filterable"": false,
      ""retrievable"": true,
      ""stored"": true,
      ""sortable"": false,
      ""facetable"": false,
      ""key"": false,
      ""synonymMaps"": []
    }},
    {{
      ""name"": ""title"",
      ""type"": ""Edm.String"",
      ""searchable"": true,
      ""filterable"": false,
      ""retrievable"": true,
      ""stored"": true,
      ""sortable"": false,
      ""facetable"": false,
      ""key"": false,
      ""synonymMaps"": []
    }},
    {{
      ""name"": ""text_vector"",
      ""type"": ""Collection(Edm.Single)"",
      ""searchable"": true,
      ""filterable"": false,
      ""retrievable"": true,
      ""stored"": true,
      ""sortable"": false,
      ""facetable"": false,
      ""key"": false,
      ""dimensions"": 1536,
      ""vectorSearchProfile"": ""{profileName}"",
      ""synonymMaps"": []
    }}
  ],
  ""scoringProfiles"": [],
  ""suggesters"": [],
  ""analyzers"": [],
  ""similarity"": {{
    ""@odata.type"": ""#Microsoft.Azure.Search.BM25Similarity""
  }},
  ""semantic"": {{
    ""defaultConfiguration"": ""{semanticName}"",
    ""configurations"": [
      {{
        ""name"": ""{semanticName}"",
        ""prioritizedFields"": {{
          ""titleField"": {{
            ""fieldName"": ""title""
          }},
          ""prioritizedContentFields"": [
            {{
              ""fieldName"": ""chunk""
            }}
          ],
          ""prioritizedKeywordsFields"": []
        }}
      }}
    ]
  }},
  ""vectorSearch"": {{
    ""algorithms"": [
      {{
        ""name"": ""{algorithmName}"",
        ""kind"": ""hnsw"",
        ""hnswParameters"": {{
          ""metric"": ""cosine"",
          ""m"": 4,
          ""efConstruction"": 400,
          ""efSearch"": 500
        }}
      }}
    ],
    ""profiles"": [
      {{
        ""name"": ""{profileName}"",
        ""algorithm"": ""{algorithmName}"",
        ""vectorizer"": ""{vectorizerName}""
      }}
    ],
    ""vectorizers"": [
      {{
        ""name"": ""{vectorizerName}"",
        ""kind"": ""azureOpenAI"",
        ""azureOpenAIParameters"": {{
          ""resourceUri"": ""{openAIEndpoint}"",
          ""deploymentId"": ""text-embedding-ada-002"",
          ""apiKey"": ""{openAIKey}"",
          ""modelName"": ""text-embedding-ada-002""
        }}
      }}
    ],
    ""compressions"": []
  }}
}}
";
            // Send index payload to Azure Search and log success/failure
            using var client = await AzureSearchHttpClientFactory.CreateAsync();
            var content = new StringContent(indexPayload, Encoding.UTF8, "application/json");
            var response = await client.PutAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Index created successfully.");
                return true;
            }
            else
            {
                Console.WriteLine($"Index creation failed: {await response.Content.ReadAsStringAsync()}");
                return false;
            }
        }


        // Create an indexer that connects a data source, skillset, and target index
        public static async Task<bool> CreateIndexerAsync(
            string searchServiceName,
            string indexerName,
            string dataSourceName,
            string skillsetName,
            string indexName)
        {
            string endpoint = $"https://{searchServiceName}.search.windows.net/indexers/{indexerName}?api-version=2024-07-01";

            // If needing to change schedule, can find information here:
            // https://learn.microsoft.com/en-au/azure/search/search-howto-schedule-indexers?tabs=rest
            string indexerPayload = $@"
{{
  ""name"": ""{indexerName}"",
  ""dataSourceName"": ""{dataSourceName}"",
  ""skillsetName"": ""{skillsetName}"",
  ""targetIndexName"": ""{indexName}"",
  ""schedule"": {{           
    ""interval"": ""P1D""
  }},
  ""fieldMappings"": [
    {{
      ""sourceFieldName"": ""metadata_storage_name"",
      ""targetFieldName"": ""title""
    }}
  ],
  ""outputFieldMappings"": [],
  ""parameters"": {{
    ""configuration"": {{
      ""dataToExtract"": ""contentAndMetadata"",
      ""parsingMode"": ""default""
    }}
  }}
}}
";

            using var client = await AzureSearchHttpClientFactory.CreateAsync();
            var content = new StringContent(indexerPayload, Encoding.UTF8, "application/json");
            var response = await client.PutAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Indexer created successfully.");
                return true;
            }
            else
            {
                Console.WriteLine($"Indexer creation failed: {await response.Content.ReadAsStringAsync()}");
                return false;
            }
        }

        // Deletes the index. Use only if we need to rollback due to a failed index creation
        public static async Task<bool> DeleteIndexAsync(string searchServiceName, string indexName)
        {
            string endpoint = $"https://{searchServiceName}.search.windows.net/indexes/{indexName}?api-version=2024-07-01";

            using var client = await AzureSearchHttpClientFactory.CreateAsync();
            var response = await client.DeleteAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Index {indexName} deleted successfully.");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to delete index {indexName}: {await response.Content.ReadAsStringAsync()}");
                return false;
            }
        }

        // Deletes the Data Source. Use only if we need to rollback due to a failed index creation
        public static async Task<bool> DeleteDataSourceAsync(string searchServiceName, string dataSourceName)
        {
            string endpoint = $"https://{searchServiceName}.search.windows.net/datasources/{dataSourceName}?api-version=2024-07-01";

            using var client = await AzureSearchHttpClientFactory.CreateAsync();
            var response = await client.DeleteAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Data source {dataSourceName} deleted successfully.");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to delete data source {dataSourceName}: {await response.Content.ReadAsStringAsync()}");
                return false;
            }
        }

        // Deletes the skillset. Use only if we need to rollback due to a failed index creation
        public static async Task<bool> DeleteSkillsetAsync(string searchServiceName, string skillsetName)
        {
            string endpoint = $"https://{searchServiceName}.search.windows.net/skillsets/{skillsetName}?api-version=2024-07-01";

            using var client = await AzureSearchHttpClientFactory.CreateAsync();
            var response = await client.DeleteAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Skillset {skillsetName} deleted successfully.");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to delete skillset {skillsetName}: {await response.Content.ReadAsStringAsync()}");
                return false;
            }
        }

        // Deletes the Indexer. Isn't currently used in production, but left here in case it is desired in some other fashion. 
        public static async Task<bool> DeleteIndexerAsync(string searchServiceName, string indexerName)
        {
            string endpoint = $"https://{searchServiceName}.search.windows.net/indexers/{indexerName}?api-version=2024-07-01";

            using var client = await AzureSearchHttpClientFactory.CreateAsync();
            var response = await client.DeleteAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Indexer {indexerName} deleted successfully.");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to delete indexer {indexerName}: {await response.Content.ReadAsStringAsync()}");
                return false;
            }
        }
    }


}
