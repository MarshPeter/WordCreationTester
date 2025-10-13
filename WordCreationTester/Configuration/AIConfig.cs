using DocumentFormat.OpenXml.Wordprocessing;

namespace WordCreationTester.Configuration
{
    public class AIConfig
    {
        public string AIEndpoint { get; }           // Key for AI Endpoint
        public string AIKey { get; }                // Key for Open AI service
        public string LLMAIName { get; }            // The name of the AI that does the Report Generation
        public string EmbeddedModelAIName { get; }  // Embeded Model Name, this does vectorization of data for indexes. 
        public string SearchEndpoint { get; }       // AI Search Endpoint
        public string SearchKey { get; }            // AI Search Key
        public string BlobConnectionString { get; } // Blob connection string
        public string DbConnectionString { get; }   // DB Connection String
        public string TenantId { get; }

        public AIConfig(string aiEndpoint, string aiKey, string searchKey, string blobConnectionString, string dbConnectionString, string tenantId)
        {
            AIEndpoint = !string.IsNullOrWhiteSpace(aiEndpoint) ? aiEndpoint
                : throw new ArgumentException("AI endpoint is required", nameof(aiEndpoint));

            AIKey = !string.IsNullOrWhiteSpace(aiKey) ? aiKey
                : throw new ArgumentException("AI key is required", nameof(aiKey));

            SearchKey = !string.IsNullOrWhiteSpace(searchKey) ? searchKey
                : throw new ArgumentException("Search key is required", nameof(searchKey));

            BlobConnectionString = !string.IsNullOrWhiteSpace(blobConnectionString) ? blobConnectionString
                : throw new ArgumentException("Blob connection string is required", nameof(blobConnectionString));

            DbConnectionString = !string.IsNullOrWhiteSpace(dbConnectionString) ? dbConnectionString
                : throw new ArgumentException("DB connection string is required", nameof(dbConnectionString));
            TenantId = !string.IsNullOrWhiteSpace(tenantId) ? tenantId
                : throw new ArgumentException("Tenant Id is required", nameof(tenantId));
            SearchEndpoint = $"https://{TenantId}-ai-search-reports.search.windows.net";
            LLMAIName = "gpt-4o-mini";
            EmbeddedModelAIName = "text-embedding-ada-002";
        }

        // Factory method for creating config from environment variables
        public static AIConfig FromEnvironment() => new AIConfig(
            Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                ?? throw new InvalidOperationException("Missing AZURE_AI_SEARCH_ENDPOINT"),
            Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
                ?? throw new InvalidOperationException("Missing AZURE_OPENAI_API_KEY"),
            Environment.GetEnvironmentVariable("AZURE_SEARCH_API_KEY")
                ?? throw new InvalidOperationException("Missing AZURE_SEARCH_API_KEY"),
            Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Missing BLOB_STORAGE_CONNECTION_STRING"),
            Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Missing DB_CONNECTION_STRING"),
            Environment.GetEnvironmentVariable("AZURE_TENANT_ID")
                ?? throw new InvalidOperationException("Missing AZURE_TENANT_ID")
        );

    }
}

