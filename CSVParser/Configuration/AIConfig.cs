namespace CsvParser.Configuration
{
    public class AIConfig
    {

        //query templates settings
        public int SqlCommandTimeoutSec { get; set; } = 600;


        // csv exporter/duplicate removal settings
        public int CsvLogEvery { get; set; } = 5000;
        public int CsvMaxFieldChars { get; set; } = 2000;
        public string OutputDirectory { get; set; } = "./docs/temp_csv";


        // csv splitter setings
        public long MaxFileSizeBytes { get; set; } = 12 * 1024 * 1024; // 12 MB limit
        public int SizeCheckInterval { get; set; } = 5000; // Check size before crate new part file, every N rows


       // uploader setings
        public string BlobContainerBaseName { get; set; } = "reports";



        public string LLMAIEndpoint { get; }
        public string LLMAIKey { get; }
        public string EmbedAIEndpoint { get; }
        public string EmbedAIKey { get; }
        public string SearchEndpoint { get; }
        public string SearchKey { get; }
        public string BlobConnectionString { get; }
        public string DbConnectionString { get; }
        public string TenantId { get; }


        public AIConfig()
        {
            LLMAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                ?? throw new InvalidOperationException("Missing AZURE_OPENAI_ENDPOINT");

            LLMAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
                ?? throw new InvalidOperationException("Missing AZURE_OPENAI_API_KEY");

            EmbedAIEndpoint = Environment.GetEnvironmentVariable("EMBED_MODEL_ENDPOINT")
                ?? throw new InvalidOperationException("Missing EMBED_MODEL_ENDPOINT");

            EmbedAIKey = Environment.GetEnvironmentVariable("EMBED_MODEL_KEY")
                ?? throw new InvalidOperationException("Missing EMBED_MODEL_KEY");

            SearchKey = Environment.GetEnvironmentVariable("AZURE_SEARCH_API_KEY")
                ?? throw new InvalidOperationException("Missing AZURE_SEARCH_API_KEY");

            BlobConnectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Missing BLOB_STORAGE_CONNECTION_STRING");

            DbConnectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Missing DB_CONNECTION_STRING");

            TenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID")
                ?? throw new InvalidOperationException("Missing AZURE_TENANT_ID");

            SearchEndpoint = $"https://{TenantId}-ai-search-reports.search.windows.net";
        }

    }
}