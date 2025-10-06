using WordCreationTester.Configuration;

namespace WordCreationTester.Services
{
    public static class StatusLogger
    {
        public static async Task LogStatusAsync(Guid aiRequestId, int status, string description)
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

            await dbContext.SaveChangesAsync();

            Console.WriteLine($"[Status] {status}: {description}");
        }
    }


}
