using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CsvParser.Configuration;

namespace CsvParser.Services
{
    public class AzureUploadService
    {
        private readonly ILogger<AzureUploadService> _logger;
        private readonly AIConfig _settings;

        public AzureUploadService(ILogger<AzureUploadService> logger, IOptions<AIConfig> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task UploadFileAsync(string filePath, string blobName, string indexName)
        {
            string connectionString = _settings.BlobConnectionString;

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_settings.BlobContainerBaseName);

            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync();

            // Define the "sub-directory" using indexName
            string subDirectory = indexName + "/";

            // --- Clear out the "folder" (delete all blobs under the prefix) ---
            await foreach (var blob in containerClient.GetBlobsAsync(prefix: subDirectory))
            {
                await containerClient.DeleteBlobIfExistsAsync(blob.Name);
            }

            // Upload file into this "sub-directory"
            string blobPath = subDirectory + blobName; // e.g. "indexA/myfile.json"
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