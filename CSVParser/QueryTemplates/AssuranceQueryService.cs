using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CsvParser.Configuration;
using CSVParser.Data;
using CsvParser.Data.Models;

namespace CsvParser.QueryTemplates
{
    public class AssuranceQueryService
    {
        private readonly TMRRadzenContext _dbContext;
        private readonly AIConfig _settings;

        public AssuranceQueryService(
            TMRRadzenContext dbContext,
            IOptions<AIConfig> settings)
        {
            _dbContext = dbContext;
            _settings = settings.Value;
        }

        public IQueryable<AssuranceCsvRow> GetQuery()
        {
            _dbContext.Database.SetCommandTimeout(_settings.SqlCommandTimeoutSec);

            var structured = from a in _dbContext.AssuranceSubmissionProcesseds
                             join s in _dbContext.AssuranceSubmissionProcessedResponsesStructureds
                                 on a.Id equals s.AssuranceSubmissionProcessedId
                             join d in _dbContext.Divisions on a.DivisionId equals d.Id
                             join u in _dbContext.AspNetUsers on a.CreatedById equals u.Id
                             join atemp in _dbContext.AssuranceTemplates on a.AssuranceTemplateId equals atemp.Id
                             join al in _dbContext.AssuranceSubmissionLogs on a.AssuranceSubmissionLogId equals al.Id
                             join ap in _dbContext.AssurancePrograms on al.AssuranceProgramId equals ap.Id
                             join sa0 in _dbContext.AssuranceSubmissionprocessedResponsesStructuredAnswers
                                 on s.Id equals sa0.AssuranceSubmissionProcessedResponsesStructuredId into saGroup
                             from sa in saGroup.DefaultIfEmpty()
                             join c0 in _dbContext.AssuranceSubmissionProcessedComments
                                 on a.Id equals c0.AssuranceSubmissionProcessedId into cGroup
                             from c in cGroup.DefaultIfEmpty()
                             join w0 in _dbContext.AssuranceSubmissionProcessedRisksStandardsActsWeightings
                                 on a.Id equals w0.AssuranceSubmissionprocessedId into wGroup
                             from w in wGroup.DefaultIfEmpty()
                             join r in _dbContext.RisksStandardsActs
                                 on w.RisksStandardsActsId equals r.Id
                             select new AssuranceCsvRow
                             {
                                 Location = d.Name,
                                 UserName = u.UserName,
                                 AssuranceTemplate = atemp.Name,
                                 AssuranceProgram = ap.Name,
                                 ProcessedYear = a.CreatedDt.Year,
                                 ProcessedMonth = a.CreatedDt.Month,
                                 QuestionText = s.QuestionText,
                                 QuestionIdentifier = s.QuestionIdentifier,
                                 Answer = sa != null ? sa.Answer : null,
                                 AnswerBGColor = sa != null ? sa.AnswerBGColor : null,
                                 AnswerFGColor = sa != null ? sa.AnswerFGColor : null,
                                 Comment = c != null ? c.Comment : null,
                                 RiskStandardActName = r.Name
                             };

            var unstructured = from a in _dbContext.AssuranceSubmissionProcesseds
                               join s in _dbContext.AssuranceSubmissionProcessedResponsesUnStructureds
                                   on a.Id equals s.AssuranceSubmissionprocessedId
                               join d in _dbContext.Divisions on a.DivisionId equals d.Id
                               join u in _dbContext.AspNetUsers on a.CreatedById equals u.Id
                               join atemp in _dbContext.AssuranceTemplates on a.AssuranceTemplateId equals atemp.Id
                               join al in _dbContext.AssuranceSubmissionLogs on a.AssuranceSubmissionLogId equals al.Id
                               join ap in _dbContext.AssurancePrograms on al.AssuranceProgramId equals ap.Id
                               join sa0 in _dbContext.AssuranceSubmissionProcessedResponsesUnStructuredAnswers
                                   on s.Id equals sa0.AssuranceSubmissionProcessedResponsesUnStructuredId into saGroup
                               from sa in saGroup.DefaultIfEmpty()
                               join c0 in _dbContext.AssuranceSubmissionProcessedComments
                                   on a.Id equals c0.AssuranceSubmissionProcessedId into cGroup
                               from c in cGroup.DefaultIfEmpty()
                               select new AssuranceCsvRow
                               {
                                   Location = d.Name,
                                   UserName = u.UserName,
                                   AssuranceTemplate = atemp.Name,
                                   AssuranceProgram = ap.Name,
                                   ProcessedYear = a.CreatedDt.Year,
                                   ProcessedMonth = a.CreatedDt.Month,
                                   QuestionText = s.QuestionText,
                                   QuestionIdentifier = s.QuestionIdentifier,
                                   Answer = sa != null ? sa.Answer : null,
                                   AnswerBGColor = sa != null ? sa.AnswerBGColor : null,
                                   AnswerFGColor = sa != null ? sa.AnswerFGColor : null,
                                   Comment = c != null ? c.Comment : null,
                                   RiskStandardActName = null
                               };

            return structured.Concat(unstructured)
                .AsNoTracking();
        }
    }
}