using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Search;


namespace AIServiceDestroyer
{
    public static class AIServiceDestroyer
    {
        public static async Task DeleteSearchService(
            string resourceGroupName,
            string searchServiceName)
        {
            try
            {
                ArmClient client = new ArmClient(new DefaultAzureCredential());
                // get default subscription
                SubscriptionResource subscription =
                  await client.GetDefaultSubscriptionAsync();
                // get the resource group
                ResourceGroupResource resourceGroup =
                  await subscription
                    .GetResourceGroups()
                    .GetAsync(resourceGroupName);

                SearchServiceCollection searchServices =
                  resourceGroup.GetSearchServices();

                // first check if it exists
                bool exists =
                  await searchServices.ExistsAsync(searchServiceName);
                if (!exists)
                {
                    Console.WriteLine(
                      $"Search service '{searchServiceName}' not found in RG '{resourceGroupName}'.");
                    return;
                }

                Console.WriteLine(
                  $"Deleting search service '{searchServiceName}' in RG '{resourceGroupName}'...");
                // fetch the specific service resource
                SearchServiceResource svc =
                  await searchServices.GetAsync(searchServiceName);
                // delete and wait until completion
                await svc.DeleteAsync(Azure.WaitUntil.Completed);

                Console.WriteLine(
                  $"Successfully deleted search service '{searchServiceName}'.");
            }
            catch (RequestFailedException rfe)
            {
                Console.WriteLine(
                  $"Azure request failed (status {rfe.Status}): {rfe.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex}");
            }
        }
    }
}
