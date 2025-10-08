using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CsvParser.Configuration;
using CSVParser.Data;
using CsvParser.DTO;

namespace CsvParser.QueryTemplates
{
    public class ComplaintsOrComplimentsQueryService
    {
        private readonly TMRRadzenContext _dbContext;
        private readonly AIConfig _settings;

        public ComplaintsOrComplimentsQueryService(
            TMRRadzenContext dbContext,
            IOptions<AIConfig> settings)
        {
            _dbContext = dbContext;
            _settings = settings.Value;
        }

        public IQueryable<ComplaintsOrComplimentsCsvRow> GetQuery()
        {
            _dbContext.Database.SetCommandTimeout(_settings.SqlCommandTimeoutSec);

            var query = from cc in _dbContext.ComplaintsOrCompliments
                        join ccc in _dbContext.ComplaintsOrComplimentsComments
                            on cc.Id equals ccc.ComplaintsOrComplimentsId into commentsGroup
                        from ccc in commentsGroup.DefaultIfEmpty()
                        orderby cc.CreatedDt
                        select new ComplaintsOrComplimentsCsvRow
                        {
                            Contact = cc.Contact,
                            ContactAddress = cc.ContactAddress,
                            ContactEmail = cc.ContactEmail,
                            ContactPhone = cc.ContactPhone,
                            Commentary = cc.Commentary,
                            InitialResponse = cc.InitialResponse,
                            Outcome = cc.Outcome,
                            CreatedById = cc.CreatedById,
                            CreatedDt = cc.CreatedDt,
                            Comment = ccc != null ? ccc.Comment : null
                        };

            return query.AsNoTracking();
        }
    }
}