using Azure.Core;
using Azure.Identity;
using CsvParser.Configuration;
using CsvParser.Data.Models;
using CsvParser.DTO;
using CSVParser.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CsvParser.Services
{
    public class IndexCreatorService
    {
        private readonly TMRRadzenContext _dbContext;
        private readonly ILogger<CsvExportService> _logger;
        private readonly AppSettings _settings;

        public IndexCreatorService(
            TMRRadzenContext dbContext,
            ILogger<CsvExportService> logger,
            IOptions<AppSettings> settings)
        {
            _dbContext = dbContext;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<bool> CreateIndex(string indexName)
        {
            string searchServiceName = "swintesting-ai-programmatic-showcase";
            string dataSourceName = "my-test";
            string skillsetName = "my-test";
            // indexName = "my-index";
            string indexerName = "my-test";
            string resourceGroup = "TMRRadzen";
            string blobConnectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING");
            string containerName = "documents";
            string openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            string openAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");


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
            bool datasourceCreated = await CreateDataSourceAsync(searchServiceName, dataSourcePayload);

            if (!datasourceCreated)
            {
                Console.WriteLine($@"The datasource {dataSourceName} was not created in {searchServiceName}");
                return false;
            }

            //// 3.Create Index
            bool indexCreated = await CreateIndexAsync(searchServiceName, indexName, openAIEndpoint, openAIKey);

            if (!indexCreated)
            {
                Console.WriteLine($@"The index {dataSourceName} was not created in {searchServiceName}");
                return false;
            }

            //// 2.Create Skillset
            bool skillsetCreated = await CreateSkillsetAsync(searchServiceName, skillsetName, openAIEndpoint, openAIKey, indexName);

            if (!skillsetCreated)
            {
                Console.WriteLine($@"The skillset {dataSourceName} was not created in {searchServiceName}");
                return false;
            }


            //// 4.Create Indexer
            bool indexerCreated = await CreateIndexerAsync(
                searchServiceName,
               indexerName,
               dataSourceName,
               skillsetName,
               indexName);

            if (!skillsetCreated)
            {
                Console.WriteLine($@"The indexer {dataSourceName} was not created in {searchServiceName}");
                return false;
            }

            return true;
        }

        public async Task<bool> UpdateDatabaseIndexInformation(
            List<IndexDefintion> createdIndexes,
            string tenantId,
            CancellationToken ct = default)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            var newlyCreatedIndexes = new List<string>();

            try
            {
                var indexNames = createdIndexes.Select(x => x.IndexName).ToList();

                // 1. Get all AIReportIndexes for the given names
                var existingReportIndexes = await _dbContext.AIReportIndexes
                    .Where(r => indexNames.Contains(r.IndexName))
                    .ToListAsync(ct);

                var reportIndexMap = existingReportIndexes.ToDictionary(r => r.IndexName, r => r);

                // 2. Get all existing tenant-index mappings (join across all 3 tables)
                var existingMappings = await (
                    from doc in _dbContext.AICSVDocuments
                    join link in _dbContext.AIReportTenantIndexes on doc.Id equals link.AIDocumentId
                    join idx in _dbContext.AIReportIndexes on link.AIReportIndexId equals idx.Id
                    where doc.TenantId.ToString().Equals(tenantId)
                    select new
                    {
                        Document = doc,
                        ReportIndex = idx
                    }
                ).ToListAsync(ct);

                var now = DateTime.UtcNow;
                var newDocs = new List<AICSVDocuments>();
                var newTenantIndexes = new List<AIReportTenantIndexes>();

                foreach (var indexDef in createdIndexes)
                {
                    // --- AIReportIndexes ---
                    AIReportIndexes reportIndex;
                    if (!reportIndexMap.TryGetValue(indexDef.IndexName, out reportIndex))
                    {
                        // Create new AIReportIndex
                        reportIndex = new AIReportIndexes
                        {
                            Id = Guid.NewGuid(),
                            IndexName = indexDef.IndexName,
                            IndexDescription = indexDef.IndexDescription
                        };
                        _dbContext.AIReportIndexes.Add(reportIndex);
                        reportIndexMap[indexDef.IndexName] = reportIndex;

                        newlyCreatedIndexes.Add(indexDef.IndexName);
                    }
                    else
                    {
                        // Update description if changed
                        if (reportIndex.IndexDescription != indexDef.IndexDescription)
                        {
                            reportIndex.IndexDescription = indexDef.IndexDescription;
                            _dbContext.AIReportIndexes.Update(reportIndex);
                        }
                    }

                    // --- AICSVDocuments + AIReportTenantIndexes ---
                    var mapping = existingMappings.FirstOrDefault(m => m.ReportIndex.IndexName == indexDef.IndexName);

                    if (mapping == null)
                    {
                        bool res = await CreateIndex(indexDef.IndexName);

                        // Create new document for this tenant
                        var doc = new AICSVDocuments
                        {
                            Id = Guid.NewGuid(),
                            TenantId = System.Guid.Parse(tenantId),
                            LastUpdatedTimestamp = now
                        };
                        newDocs.Add(doc);

                        // Create join link
                        newTenantIndexes.Add(new AIReportTenantIndexes
                        {
                            AIDocumentId = doc.Id,
                            AIReportIndexId = reportIndex.Id
                        });

                        if (!newlyCreatedIndexes.Contains(indexDef.IndexName))
                            newlyCreatedIndexes.Add(indexDef.IndexName);
                    }
                    else
                    {
                        // Update timestamp on existing doc
                        mapping.Document.LastUpdatedTimestamp = now;
                        _dbContext.AICSVDocuments.Update(mapping.Document);
                    }
                }

                // Save new docs
                if (newDocs.Any())
                    await _dbContext.AICSVDocuments.AddRangeAsync(newDocs, ct);

                // Save new tenant-index links
                if (newTenantIndexes.Any())
                    await _dbContext.AIReportTenantIndexes.AddRangeAsync(newTenantIndexes, ct);

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation(
                    "Updated index information for tenant {TenantId}. Newly created indexes: {Indexes}",
                    tenantId,
                    string.Join(", ", newlyCreatedIndexes));

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error updating index information for tenant {TenantId}", tenantId);
                return false;
            }
        }
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

            // Set the Authorization header
            httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Token);

            // Set the Content-Type header
            var content = new StringContent(dataSourcePayload, Encoding.UTF8, "application/json");

            // Send the request
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

        public static async Task<bool> CreateSkillsetAsync(
        string searchServiceName,
        string skillsetName,
        string openAIEndpoint,
        string openAIKey,
        string indexName)
        {
            string endpoint = $"https://{searchServiceName}.search.windows.net/skillsets/{skillsetName}?api-version=2024-07-01";
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


        public static async Task<bool> CreateIndexerAsync(
            string searchServiceName,
            string indexerName,
            string dataSourceName,
            string skillsetName,
            string indexName)
        {
            string endpoint = $"https://{searchServiceName}.search.windows.net/indexers/{indexerName}?api-version=2024-07-01";

            string indexerPayload = $@"
{{
  ""name"": ""{indexerName}"",
  ""dataSourceName"": ""{dataSourceName}"",
  ""skillsetName"": ""{skillsetName}"",
  ""targetIndexName"": ""{indexName}"",
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

            // NOTE: To add a schedule add the following code to the above payload
            //""schedule"": {
            //    {
            //        ""interval"": ""P1D"", // Every 1 day
            //        ""startTime"": ""2024 - 06 - 01T00: 00:00Z""
            //    }
            //}

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
    }
}
