using Azure.AI.OpenAI.Chat;
using Azure.AI.OpenAI;
using Azure;
using OpenAI.Chat;
using static System.Environment;

namespace WordCreationTester
{
    public static class AIRunner
    {
        public static void runAI()
        {
            string AI_endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "https://<your-resource-name>.openai.azure.com/";
            string AI_key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "<your-key>";
            string searchEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_ENDPOINT");
            string searchKey = Environment.GetEnvironmentVariable("AZURE_SEARCH_API_KEY") ?? "<your-search-api-key>";


            var searchIndexes = new List<string>
        {
            "gptindex",
            "incident-index",
            // "Give me all incident descriptions tied to the automatedServices email."
        };

            var responses = new List<string> { };

            foreach (String index in searchIndexes)
            {
                try
                {
                    // Use the recommended keyless credential instead of the AzureKeyCredential credential.
                    // AzureOpenAIClient openAIClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
                    AzureOpenAIClient openAIClient = new AzureOpenAIClient(new Uri(AI_endpoint), new AzureKeyCredential(AI_key));

                    // This must match the custom deployment name you chose for your model
                    ChatClient chatClient = openAIClient.GetChatClient("gpt-4o-mini");

                    ChatCompletionOptions options = new ChatCompletionOptions();

#pragma warning disable AOAI001 // Suppress the diagnostic warning  

                    options.AddDataSource(new AzureSearchChatDataSource()
                    {
                        Endpoint = new Uri(searchEndpoint),
                        IndexName = index,
                        Authentication = DataSourceAuthentication.FromApiKey(searchKey), // Add your Azure AI Search admin key here  
                    });

                    Console.WriteLine(options.GetDataSources());

                    ChatCompletion completion = chatClient.CompleteChat([
                        new SystemChatMessage("You are a helpful assistant that collects information that we request by looking at the provided datasources. If you find nothing of relevance in the datasource, just return an empty string and nothing else. This is fine and shouldn't be seen as an error."),
                new UserChatMessage("Show all incidents involving luke")
                    ], options);

                    responses.Add(completion.Content[0].Text);
                }
                catch (Exception e)
                {
                    Console.WriteLine("nothing found here.");
                }
            }

            foreach (string res in responses)
            {
                Console.WriteLine(res);
            }
        }
    }
}
