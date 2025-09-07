using Azure.Messaging.ServiceBus;
using System;
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

            try
            {
                // --- Save payload to Database ---
                using (var dbContext = new PayloadDbConnection())
                {
                    var entity = new AIReportRequestEntity
                    {
                        AIRequestId = payload.AIRequestId,
                        TenantId = payload.TenantId,
                        CreatedBy = payload.CreatedBy,
                        DateFrom = payload.DateFrom,
                        DateTo = payload.DateTo,
                        ReportType = payload.ReportType,
                        ReportCategories = JsonSerializer.Serialize(payload.ReportCategories ?? new List<string>(), jsonOptions),
                        ReportStatements = JsonSerializer.Serialize(payload.ReportStatements?.Select(s => s.Text).ToList() ?? new List<string>(), jsonOptions),
                        IndexType = payload.IndexType,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    };

                    dbContext.AIReportRequests.Add(entity);
                    await dbContext.SaveChangesAsync();

                    Console.WriteLine($"Payload saved to database with AIRequestId: {payload.AIRequestId}");
                }

                // --- Log status update ---
                await StatusLogger.LogStatusAsync(payload.AIRequestId, "Pending", "Payload created and stored in database.");

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
                await StatusLogger.LogStatusAsync(payload.AIRequestId, "Queued", "Minimal message sent to Service Bus for processing.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to process payload: {ex.Message}");
                await StatusLogger.LogStatusAsync(payload.AIRequestId, "Failed", $"Payload processing error: {ex.Message}");
                throw;
            }
        }
    }
}
