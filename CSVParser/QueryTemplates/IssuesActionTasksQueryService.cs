using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CsvParser.Configuration;
using CSVParser.Data;
using CsvParser.DTO;

namespace CsvParser.QueryTemplates
{
    public class IssuesActionTasksQueryService
    {
        private readonly TMRRadzenContext _dbContext;
        private readonly AIConfig _settings;

        public IssuesActionTasksQueryService(
            TMRRadzenContext dbContext,
            IOptions<AIConfig> settings)
        {
            _dbContext = dbContext;
            _settings = settings.Value;
        }

        public IQueryable<IssuesActionTasksCsvRow> GetQuery()
        {
            _dbContext.Database.SetCommandTimeout(_settings.SqlCommandTimeoutSec);

            var query = from i in _dbContext.IssuesActionsTasks
                        join d in _dbContext.Divisions on i.DivisionId equals d.Id
                        join u in _dbContext.IssuesActionsTasksUrgencyAndOffsets on i.UrgencyId equals u.Id
                        join t in _dbContext.IssuesActionTaskTypes on i.IATTypeId equals t.Id
                        join us in _dbContext.AspNetUsers on i.AllocatedToId equals us.Id
                        orderby i.CreatedDt
                        select new IssuesActionTasksCsvRow
                        {
                            Title = i.Title,
                            Status = i.Status == 1 ? "New" :
                                    i.Status == 2 ? "In Progress" :
                                    i.Status == 3 ? "Rejected" :
                                    i.Status == 4 ? "Done" : "Unknown",
                            Description = i.Description,
                            Outcome = i.Outcome,
                            IATCategory = i.IATCategory,
                            CreatedDt = i.CreatedDt,
                            DueDate = i.DueDate,
                            Location = d.Name,
                            Urgency = u.Name,
                            UrgencyDescription = u.Description,
                            Type = t.Name,
                            Allocated = us.UserName
                        };

            return query.AsNoTracking();
        }
    }
}