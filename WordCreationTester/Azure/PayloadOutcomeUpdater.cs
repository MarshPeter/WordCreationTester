using WordCreationTester.Configuration;

namespace WordCreationTester.Azure
{
    public static class PayloadOutcomeUpdater
    {
        // TODO: Transform the status into the string for logging. 
        public static async Task UpdatePayloadStatus(Guid aiRequestId, int status, string description)
        {
            var config = AIConfig.FromEnvironment();

            using var dbContext = new PayloadDbConnection(config);

            var request = await dbContext.AIReportRequest.FindAsync(aiRequestId);
            if (request == null)
            {
                Console.WriteLine($"[Status] Failed to log status. Request {aiRequestId} not found.");
                return;
            }

           
            request.Status = status;
            request.Outcome = description;
            request.OutcomeDt = DateTime.Now;

            await dbContext.SaveChangesAsync();

            Console.WriteLine($"[Status] {status}: {description}");
        }
    }


}
