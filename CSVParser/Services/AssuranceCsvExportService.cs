using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CsvParser.Configuration;
using CSVParser.Data;
using CsvParser.Data.Models;
using CsvParser.Interfaces;

namespace CsvParser.Services
{
    public class AssuranceCsvExportService : ICSVExporter
    {
        private readonly TMRRadzenContext _dbContext;
        private readonly ILogger<ICSVExporter> _logger;
        private readonly AppSettings _settings;

        public AssuranceCsvExportService(
            TMRRadzenContext dbContext,
            ILogger<ICSVExporter> logger,
            IOptions<AppSettings> settings)
        {
            _dbContext = dbContext;
            _logger = logger;
            _settings = settings.Value;
        }

        // Creates csv file in a temporary directory. Returns location of created file. 
        public async Task<string> ExportCSV(
            string csvName,
            string timestamp,
            CancellationToken ct = default)
        {
            // Temporary directory to store file until it is uploaded. 
            Directory.CreateDirectory(_settings.OutputDirectory);
            string filePath = Path.Combine(
                _settings.OutputDirectory,
                $"{csvName}-{timestamp}.csv");

            _dbContext.Database.SetCommandTimeout(_settings.SqlCommandTimeoutSec);

            // We limit rows to prevent indexes not being created.
            var query = BuildAssuranceRowsQuery()
                .AsNoTracking()
                .Take(_settings.CsvMaxRows);      // This throws warnings due to undeterministic ordering. This doesn't matter to AI, ignore it.

            await WriteCsvFileAsync(filePath, query, ct);

            _logger.LogInformation(
                "CSV export complete. File: {file_path}",
                Path.GetFullPath(filePath));

            return filePath;
        }

        // Retrieves relevant data from the database using EF. 
        private IQueryable<AssuranceCsvRow> BuildAssuranceRowsQuery()
        {
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
                                   RiskStandardActName = (string?)null
                               };

            // Unique union essentially
            return structured.Concat(unstructured);
        }

        // Takes data from query, and inserts it into a csv file located at filepath
        private async Task WriteCsvFileAsync<T>(string filePath, IQueryable<T> query, CancellationToken ct)
        {
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16);
            using var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            // Write header
            var props = typeof(T).GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                if (i > 0) sw.Write(',');
                sw.Write(EscapeCsv(props[i].Name));
            }
            await sw.WriteLineAsync();

            // Write rows
            long written = 0;
            try
            {
                await foreach (var row in query.AsAsyncEnumerable().WithCancellation(ct))
                {
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (i > 0) sw.Write(',');
                        var val = props[i].GetValue(row);
                        var s = ConvertToString(val);
                        if (s.Length > _settings.CsvMaxFieldChars)
                            s = s.Substring(0, _settings.CsvMaxFieldChars) + "…";
                        sw.Write(EscapeCsv(s));
                    }
                    await sw.WriteLineAsync();

                    written++;
                    if (written % _settings.CsvLogEvery == 0)
                    {
                        await sw.FlushAsync();
                        _logger.LogInformation("Wrote {RowCount} rows...", written);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CSV export");
                throw;
            }

            await sw.FlushAsync();
            _logger.LogInformation("CSV export complete. Total rows: {TotalRows}", written);
        }
    
    // Standardize values into specific strings
    private static string ConvertToString(object? value) =>
        value switch
        {
            null => string.Empty,
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };

    // Sanitization
    private static string EscapeCsv(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        bool mustQuote = s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
        return mustQuote ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
    }
    }
}