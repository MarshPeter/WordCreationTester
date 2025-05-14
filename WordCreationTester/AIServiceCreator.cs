using Azure.Identity;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Search;
using Azure.ResourceManager.Search.Models;
using Azure;
using Azure.Core.Diagnostics;
using System.Diagnostics.Tracing;

namespace WordCreationTester
{
    public static class AIServiceCreator
    {
        public static async Task createSearchResource()
        {

            // If the resource group doesn't exist, you can create it:
            // ResourceGroupData resourceGroupData = new ResourceGroupData(location);
            // ResourceGroupResource createdResourceGroup = await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, resourceGroupData);
            // ResourceGroupResource resourceGroup = createdResourceGroup.Value;


            // Uncomment the following for logging information
            //using var listener = new AzureEventSourceListener((eventData, text) =>
            //{
            //    Console.WriteLine($"[Azure.Identity] {text}");
            //}, EventLevel.Informational);


            // if the resource group does exist, do the following:
            string resourceGroupName = "swin-testing";
            try
            {
                ArmClient client = new ArmClient(new DefaultAzureCredential());

                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();
                ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();
                ResourceGroupResource resourceGroup = await resourceGroups.GetAsync(resourceGroupName);

                string searchServiceName = "swin-testing-ai-programmatic";

                // Azure Locations: https://learn.microsoft.com/en-us/dotnet/api/azure.core.azurelocation?view=azure-dotnet
                SearchServiceData searchServiceData = new SearchServiceData(AzureLocation.EastUS2)
                {
                    SkuName = SearchSkuName.Basic,
                    ReplicaCount = 1,
                    PartitionCount = 1,
                };

                SearchServiceCollection searchServices = resourceGroup.GetSearchServices();

                Console.WriteLine($"Creating search service '{searchServiceName}' in resource group '{resourceGroupName}'...");

                ArmOperation<SearchServiceResource> operation = await searchServices.CreateOrUpdateAsync(WaitUntil.Completed, searchServiceName, searchServiceData);

                SearchServiceResource searchService = operation.Value;

                Console.WriteLine($"Successfully created search service: {searchService.Id}");
                Console.WriteLine($"Provisioning State: {searchService.Data.ProvisioningState}");
                Console.WriteLine($"Status: {searchService.Data.Status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An Error Occurred: {ex.Message}");
            }


        }
    }
}
