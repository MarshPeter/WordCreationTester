using System;
using System.Threading.Tasks;

namespace WordCreationTester
{
    public static class StatusLogger
    {
        public static async Task LogStatusAsync(Guid aiRequestId, string status, string description)
        {
            using var dbContext = new PayloadDbConnection();

            var request = await dbContext.AIReportRequests.FindAsync(aiRequestId);
            if (request == null)
            {
                Console.WriteLine($"[Status] Failed to log status. Request {aiRequestId} not found.");
                return;
            }

            request.Status = status;
            request.StatusDescription = description;

            await dbContext.SaveChangesAsync();

            Console.WriteLine($"[Status] {status}: {description}");
        }
    }


}
