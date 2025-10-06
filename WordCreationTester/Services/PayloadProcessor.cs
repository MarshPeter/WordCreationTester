using System.Text.Json;
using WordCreationTester.Configuration;
using WordCreationTester.DTO;


namespace WordCreationTester.Services
{
    public static class PayloadProcessor
    {
        public static async Task ProcessPayloadAsync(AIReportRequest payload)
        {
            var config = AIConfig.FromEnvironment();

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
                using (var dbContext = new PayloadDbConnection(config))
                {

                    dbContext.AIReportRequest.Add(payload);
                    await dbContext.SaveChangesAsync();

                    Console.WriteLine($"Payload saved to database with AIRequestId: {payload.Id}");
                }

                // --- Log status update ---
                await StatusLogger.LogStatusAsync(payload.Id, 1, "Payload created and stored in database.");

                // --- Send minimal Service Bus message ---
                var minimalMessage = new ServiceBusRequestMessage
                {
                    TenantId = new Guid(payload.CreatedById),
                    AIRequestId = payload.Id
                };

                Console.WriteLine("Minimal message sent to Service Bus.");
                await StatusLogger.LogStatusAsync(payload.Id, 1, "Minimal message sent to Service Bus for processing.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to process payload: {ex.Message}");
                await StatusLogger.LogStatusAsync(payload.Id, 1, $"Payload processing error: {ex.Message}");
                throw;
            }
        }
    }
}
