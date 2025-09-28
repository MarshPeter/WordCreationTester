
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;

using OpenAI.Chat;

namespace WordCreationTester
{
    public static class AIRunner
    {
        public static async Task<string> RunAI(
            AIConfig config,
            string systemMessage,
            string userMessage,
            string searchIndex,
            bool dataSource = true)
        {

            try
            {
                var openAIClient = new AzureOpenAIClient(
                    new Uri(config.AIEndpoint),
                    new AzureKeyCredential(config.AIKey)
                );

                var chatClient = openAIClient.GetChatClient("gpt-4o-mini");
#pragma warning disable AOAI001 // Suppress the diagnostic warning
                var options = new ChatCompletionOptions();

                if (dataSource)
                {
                    options.AddDataSource(retrieveDataSource(config.SearchEndpoint, config.SearchKey, searchIndex));
                }

#pragma warning restore AOAI001 // Suppress the diagnostic warning
                options.Temperature = 0f;        //low= control randomness in response 
                options.TopP = 0.2f;              //low=  limits token choice for focused output
                options.FrequencyPenalty = 0.5f;  //Medium= reduce repeated words
                options.PresencePenalty = 0.2f;   //Low = mostly stays on same topics
                options.MaxOutputTokenCount = 16000;   // high= allows long responses/ Max for gpt-4o-mini is 16384
#pragma warning disable AOAI001 // Suppress the diagnostic warning

                // This is just for debugging, we can probably turn it off when we are happy with things
                if (dataSource)
                {
                    Console.WriteLine("Configured data sources:");
                    foreach (var ds in options.GetDataSources())
                    {
                        Console.WriteLine(ds);
                    }
                }

#pragma warning restore AOAI001 // Suppress the diagnostic warning
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
        private static AzureSearchChatDataSource retrieveDataSource(string searchEndpoint, string searchKey, string searchIndex)
        {
            return new AzureSearchChatDataSource()
            {
                Endpoint = new Uri(searchEndpoint),                                                      // Endpoint of Azure Search
                Authentication = DataSourceAuthentication.FromApiKey(searchKey),                         // Key from Azure Search
                IndexName = searchIndex,                                                                 // Index from Azure Search
                QueryType = DataSourceQueryType.VectorSemanticHybrid,                                    // Vector + Semantic Search = Better responses
                SemanticConfiguration = "my-index-semantic-configuration",                               // Semantic setup
                VectorizationSource = DataSourceVectorizer.FromDeploymentName("text-embedding-ada-002"), // The Text Embedding LLM Model. This is different from the LLM Model
                AllowPartialResults = true,                                                              // This allows for predictable results
            };
        }
#pragma warning restore AOAI001 // Suppress the diagnostic warning

    }
}