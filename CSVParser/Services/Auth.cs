using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;


// Creates an HTTP client preconfigured with an Azure AD access token for Azure Cognitive Search authentication
public static class AzureSearchHttpClientFactory
{
    public static async Task<HttpClient> CreateAsync()
    {
        // Acquire an Azure AD access token for authenticating requests to Azure Cognitive Search 
        var credential = new DefaultAzureCredential();
        var tokenRequestContext = new TokenRequestContext(
            new[] { "https://search.azure.com/.default" });
        var token = await credential.GetTokenAsync(tokenRequestContext);

        // Creates HTTP client with Bearer token authentication and return it ready to use. 
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Token);
        return client;
    }
}