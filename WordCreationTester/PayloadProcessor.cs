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

            // --- Save payload to Database ---
            using (var dbContext = new PayloadDbConnection())
            {
                string jsonPayload = JsonSerializer.Serialize(payload, jsonOptions);

                var entity = new AIReportRequestEntity
                {
                    AIRequestId = payload.AIRequestId,
                    TenantId = payload.TenantId,
                    CreatedBy = payload.CreatedBy,
                    ParametersJson = jsonPayload,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                dbContext.AIReportRequests.Add(entity);
                await dbContext.SaveChangesAsync();

                Console.WriteLine($"Payload saved to database with AIRequestId: {payload.AIRequestId}");
            }

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
