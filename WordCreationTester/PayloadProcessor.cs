using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WordCreationTester;

namespace WordCreationTester
{
    public static class PayloadProcessor
    {
        public static async Task ProcessPayloadAsync(AIReportRequestPayload payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            // --- Upload payload to Blob Storage ---
            string connectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING");
            string containerName = "payloads";

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            string blobName = $"payload_{payload.AIRequestId}.json";
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            string jsonPayload = JsonSerializer.Serialize(payload, jsonOptions);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonPayload));
            await blobClient.UploadAsync(ms, overwrite: true);

            Console.WriteLine($"Payload saved to blob: {blobClient.Uri}");

            // Optional: set AttachmentUrl to blob URI
            payload.AttachmentUrl = blobClient.Uri;

            // --- Send minimal Service Bus message ---
            var minimalMessage = new ServiceBusRequestMessage
            {
                TenantId = payload.TenantId,
                AIRequestId = payload.AIRequestId
            };

            //string serviceBusConnectionString = Environment.GetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING");
            //string queueName = "reports";

            //await using var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
            //ServiceBusSender sender = serviceBusClient.CreateSender(queueName);

            //var serviceBusMessage = new ServiceBusMessage(JsonSerializer.Serialize(minimalMessage, jsonOptions));
            //await sender.SendMessageAsync(serviceBusMessage);

            Console.WriteLine("Minimal message sent to Service Bus.");
        }
    }
}
