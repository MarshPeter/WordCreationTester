using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CsvParser.Configuration;

namespace CsvParser.Services
{
    public class CsvAzureUploadService
    {
        private readonly ILogger<CsvAzureUploadService> _logger;
        private readonly AIConfig _settings;

        public CsvAzureUploadService(ILogger<CsvAzureUploadService> logger, IOptions<AIConfig> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }


        // Clears all blobs in the specified index folder 
        public async Task ClearIndexFolderAsync(string indexName)
        {
            string connectionString = _settings.BlobConnectionString;

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_settings.BlobContainerBaseName);

            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync();

            // Define the "sub-directory" using indexName
            string subDirectory = indexName + "/";

            _logger.LogInformation("Clearing existing blobs from folder: {IndexName}", indexName);

            // Delete all blobs under the prefix
            int deletedCount = 0;
            await foreach (var blob in containerClient.GetBlobsAsync(prefix: subDirectory))
            {
                await containerClient.DeleteBlobIfExistsAsync(blob.Name);
                deletedCount++;
            }

            _logger.LogInformation("Deleted {Count} existing blob(s) from {IndexName} folder", deletedCount, indexName);
        }

        // Uploads a file to Azure Blob Storage WITHOUT clearing the folder first
       
        public async Task UploadFileAsync(string filePath, string blobName, string indexName)
        {
            string connectionString = _settings.BlobConnectionString;

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_settings.BlobContainerBaseName);

            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync();

            // Define the "sub-directory" using indexName
            string subDirectory = indexName + "/";

            // Upload file into this "sub-directory"
            string blobPath = subDirectory + blobName; // e.g. "indexA/myfile.csv"
            var blobClient = containerClient.GetBlobClient(blobPath);

            _logger.LogInformation(
                "Uploading {FileName} to sub-directory {IndexName} in Azure Blob Storage...",
                blobName, indexName
            );

            using var fileStream = File.OpenRead(filePath);
            await blobClient.UploadAsync(fileStream, overwrite: true);

            _logger.LogInformation(
                "Successfully uploaded {BlobName} to {IndexName} folder in Azure Blob Storage",
                blobPath, indexName
            );
        }
    }
}