using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Core;

public static class AzureSearchDataSourceCreator
{
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
