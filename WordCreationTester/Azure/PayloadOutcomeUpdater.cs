using WordCreationTester.Configuration;

namespace WordCreationTester.Azure
{
    public static class PayloadOutcomeUpdater
    {
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

            string statusString = "";

            switch (status)
            {
                case 1:
                    statusString = "Queued";
                    break;
                case 2:
                    statusString = "In Progress";
                    break;
                case 3:
                    statusString = "Complete Success";
                    break;
                case 4:
                    statusString = "Failed";
                    break;
            }

            Console.WriteLine($"[Status] {statusString}: {description}");
        }
    }


}
