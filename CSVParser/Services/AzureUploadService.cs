using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CsvParser.Configuration;

namespace CsvParser.Services
{
    public class AzureUploadService
    {
        private readonly ILogger<AzureUploadService> _logger;
        private readonly AppSettings _settings;

        public AzureUploadService(ILogger<AzureUploadService> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task UploadFileAsync(string filePath, string blobName)
        {
            string connectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING")
                ?? throw new InvalidOperationException("BLOB_STORAGE_CONNECTION_STRING environment variable is not set");

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_settings.BlobContainerName);

            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Uploading {FileName} to Azure Blob Storage...", blobName);

            using var fileStream = File.OpenRead(filePath);
            await blobClient.UploadAsync(fileStream, overwrite: true);

            _logger.LogInformation("Successfully uploaded {BlobName} to Azure Blob Storage", blobName);
        }
    }
}