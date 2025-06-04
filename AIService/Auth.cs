using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;

public static class AzureSearchHttpClientFactory
{
    public static async Task<HttpClient> CreateAsync()
    {
        var credential = new DefaultAzureCredential();
        var tokenRequestContext = new TokenRequestContext(
            new[] { "https://search.azure.com/.default" });
        var token = await credential.GetTokenAsync(tokenRequestContext);

        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Token);
        return client;
    }
}