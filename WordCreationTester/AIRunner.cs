using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using OpenAI.Chat;

namespace WordCreationTester
{
    public static class AIRunner
    {
        public static async Task runAI()
        {
            string AI_endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "https://<your-resource-name>.openai.azure.com/";
            string AI_key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "<your-key>";
            string searchEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_ENDPOINT");
            string searchKey = Environment.GetEnvironmentVariable("AZURE_SEARCH_API_KEY") ?? "<your-search-api-key>";
            var searchIndex = "document-assurance-view-index";

            try
            {
                var openAIClient = new AzureOpenAIClient(
                    new Uri(AI_endpoint),
                    new AzureKeyCredential(AI_key)
                );

                var chatClient = openAIClient.GetChatClient("gpt-4o-mini");
#pragma warning disable AOAI001 // Suppress the diagnostic warning
                var options = new ChatCompletionOptions();
                options.AddDataSource(new AzureSearchChatDataSource()
                {
                    Endpoint = new Uri(searchEndpoint),
                    Authentication = DataSourceAuthentication.FromApiKey(searchKey),
                    IndexName = searchIndex,
                    QueryType = DataSourceQueryType.VectorSemanticHybrid,
                    SemanticConfiguration = "document-assurance-view-index-semantic-configuration",
                    VectorizationSource = DataSourceVectorizer.FromDeploymentName("text-embedding-ada-002"),
                    AllowPartialResults = true,
                });
#pragma warning restore AOAI001 // Suppress the diagnostic warning
                options.Temperature = 0.7f;
                options.TopP = 0.95f;
                options.FrequencyPenalty = 0f;
                options.PresencePenalty = 0f;
                options.MaxOutputTokenCount = 800;
#pragma warning disable AOAI001 // Suppress the diagnostic warning
                Console.WriteLine("Configured data sources:");
                foreach (var ds in options.GetDataSources())
                {
                    Console.WriteLine(ds);
                }
#pragma warning restore AOAI001 // Suppress the diagnostic warning
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are an AI assistant that helps people find information."),
                    new UserChatMessage("Show me a summary of all comments made about medication safety")
                };

                ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);

                Console.WriteLine(completion.Content[0].Text);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}