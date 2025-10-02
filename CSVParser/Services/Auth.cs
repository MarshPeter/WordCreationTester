using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;


//creates an HTTPclient preconfigured with an Azure AD  access token
public static class AzureSearchHttpClientFactory
{
    public static async Task<HttpClient> CreateAsync()
    {
        // takes an Azure access token so can authenticate request  to cognitive search. 
        var credential = new DefaultAzureCredential();
        var tokenRequestContext = new TokenRequestContext(
            new[] { "https://search.azure.com/.default" });
        var token = await credential.GetTokenAsync(tokenRequestContext);

        // creates HTTP client attach the token for authentication and return it ready to use. 
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Token);
        return client;
    }
}