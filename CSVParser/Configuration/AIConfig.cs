namespace CsvParser.Configuration
{
    public class AIConfig
    {
        public int CsvMaxRows { get; set; } = 50000;
        public int CsvLogEvery { get; set; } = 5000;
        public int CsvMaxFieldChars { get; set; } = 2000;
        public int SqlCommandTimeoutSec { get; set; } = 600;
        public string OutputDirectory { get; set; } = "./docs/temp_csv";
        public string BlobContainerBaseName { get; set; } = "reports";
        public string LLMAIEndpoint { get; }
        public string LLMAIKey { get; }
        public string EmbedAIEndpoint { get; }
        public string EmbedAIKey { get; }
        public string SearchEndpoint { get; }
        public string SearchKey { get; }
        public string BlobConnectionString { get; }
        public string DbConnectionString { get; }

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

            SearchEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_ENDPOINT")
                ?? throw new InvalidOperationException("Missing AZURE_AI_SEARCH_ENDPOINT");

            SearchKey = Environment.GetEnvironmentVariable("AZURE_SEARCH_API_KEY")
                ?? throw new InvalidOperationException("Missing AZURE_SEARCH_API_KEY");

            BlobConnectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Missing BLOB_STORAGE_CONNECTION_STRING");

            DbConnectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Missing DB_CONNECTION_STRING");
        }

    }
}