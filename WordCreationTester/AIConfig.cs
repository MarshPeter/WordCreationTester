namespace WordCreationTester
{
    public class AIConfig
    {
        public string AIEndpoint { get; }
        public string AIKey { get; }
        public string SearchEndpoint { get; }
        public string SearchKey { get; }

        public AIConfig(string aiEndpoint, string aiKey, string searchEndpoint, string searchKey)
        {
            AIEndpoint = !string.IsNullOrWhiteSpace(aiEndpoint) ? aiEndpoint
                : throw new ArgumentException("AI endpoint is required", nameof(aiEndpoint));

            AIKey = !string.IsNullOrWhiteSpace(aiKey) ? aiKey
                : throw new ArgumentException("AI key is required", nameof(aiKey));

            SearchEndpoint = !string.IsNullOrWhiteSpace(searchEndpoint) ? searchEndpoint
                : throw new ArgumentException("Search endpoint is required", nameof(searchEndpoint));

            SearchKey = !string.IsNullOrWhiteSpace(searchKey) ? searchKey
                : throw new ArgumentException("Search key is required", nameof(searchKey));
        }

        // Factory method for creating config from environment variables
        public static AIConfig FromEnvironment() => new AIConfig(
            Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                ?? throw new InvalidOperationException("Missing AZURE_OPENAI_ENDPOINT"),
            Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
                ?? throw new InvalidOperationException("Missing AZURE_OPENAI_API_KEY"),
            Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_ENDPOINT")
                ?? throw new InvalidOperationException("Missing AZURE_AI_SEARCH_ENDPOINT"),
            Environment.GetEnvironmentVariable("AZURE_SEARCH_API_KEY")
                ?? throw new InvalidOperationException("Missing AZURE_SEARCH_API_KEY")
        );

    }
}

