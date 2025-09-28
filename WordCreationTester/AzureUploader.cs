using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WordCreationTester
{
    public static class AzureUploader
    {
        public static async Task<string> UploadReportAsync(string filePath, string blobName, AIConfig config)
        {
            string connectionString = config.BlobConnectionString;

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Azure Storage connection string is not set.");

            string containerName = "reports";

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(blobName);

            using FileStream uploadFileStream = File.OpenRead(filePath);
            await blobClient.UploadAsync(uploadFileStream, overwrite: true);
            uploadFileStream.Close();

            Console.WriteLine($"Uploaded '{blobName}' to Azure Blob Storage.");
            return blobClient.Uri.ToString();
        }
    }
}
