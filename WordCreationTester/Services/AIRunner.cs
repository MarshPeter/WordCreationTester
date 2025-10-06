using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;

using OpenAI.Chat;
using WordCreationTester.Configuration;

namespace WordCreationTester.Services
{

    // Defines the format of the AI's response output
    public enum OutputFormat
    {
        PlainText,  //Plain text format - natural language responses
        Json        // JSON format - structured data with formatting overhead
    }

    public static class AIRunner
    {
        // Sends a request to Azure OpenAI and returns the AI-generated response
        public static async Task<string> RunAI(
            AIConfig config,
            string systemMessage,
            string userMessage,
            string searchIndex,
            bool dataSource = true,
            OutputFormat outputFormat = OutputFormat.PlainText)
        {
            try
            {
                var openAIClient = new AzureOpenAIClient(
                    new Uri(config.AIEndpoint),
                    new AzureKeyCredential(config.AIKey)
                );

                var chatClient = openAIClient.GetChatClient(config.LLMAIName);
#pragma warning disable AOAI001 // Suppress the diagnostic warning
                var options = new ChatCompletionOptions();

                if (dataSource)
                {
                    options.AddDataSource(RetrieveDataSource(config.SearchEndpoint, config.SearchKey, searchIndex, config));
                }

#pragma warning restore AOAI001 // Suppress the diagnostic warning
                options.Temperature = 0.7f;                          //low= control randomness in response 
                options.TopP = 0.95f;                                //low=  limits token choice for focused output
                options.FrequencyPenalty = 0f;                       //Medium= reduce repeated words
                options.PresencePenalty = 0f;                        //Low = mostly stays on same topics

                                                                    // Set max output tokens based on format. JSON needs more tokens due to brackets, quotes, and formatting
                options.MaxOutputTokenCount = outputFormat == OutputFormat.Json
                    ? 16000                                         // JSON: use full capacity
                    : 12800;                                        // Plain text: 80% as JSON is more token expensive

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemMessage),
                    new UserChatMessage(userMessage)
                };

                ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);


                return completion.Content[0].Text;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("AI was unable to generate response:", e);
            }
        }

#pragma warning disable AOAI001 // suppress experimental warning of AzureSearchChatDataSource - it is promoted as the primary method,
        // But is not seen as stable at this stage according to official documentation. 

        // This gives permission to the LLM model to communicate with our AI Search instance
        private static AzureSearchChatDataSource RetrieveDataSource(string searchEndpoint, string searchKey, string searchIndex, AIConfig config)
        {
            Console.WriteLine($"API KEY: {searchKey}");
            Console.WriteLine($"SEARCH ENDPOINT: {searchEndpoint}");
            return new AzureSearchChatDataSource()
            {
                Endpoint = new Uri(searchEndpoint),                                                      // Endpoint of Azure Search
                Authentication = DataSourceAuthentication.FromApiKey(searchKey),                         // Key from Azure Search
                IndexName = searchIndex,                                                                 // Index from Azure Search
                QueryType = DataSourceQueryType.VectorSemanticHybrid,                                    // Vector + Semantic Search = Better responses
                SemanticConfiguration = $"{searchIndex}-semantic-configuration",                         // Semantic setup
                VectorizationSource = DataSourceVectorizer.FromDeploymentName(config.EmbeddedModelAIName), // The Text Embedding LLM Model. This is different from the LLM Model
                AllowPartialResults = true,                                                              // This allows for predictable results
            };
        }
#pragma warning restore AOAI001 // Suppress the diagnostic warning

    }
}
