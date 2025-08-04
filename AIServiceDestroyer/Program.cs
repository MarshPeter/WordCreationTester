using AIServiceDestroyer;

string resourceGroup = "TMRRadzen";
string searchServiceName = "swintesting-ai-programmatic-showcase";


await AIServiceDestroyer.AIServiceDestroyer.DeleteSearchService(resourceGroup, searchServiceName);