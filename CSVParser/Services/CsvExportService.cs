using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CsvParser.Configuration;
using CSVParser.Data;
using CsvParser.Data.Models;


namespace CsvParser.Services
{
    public class CsvExportService
    {
        private readonly TMRRadzenContext _dbContext;
        private readonly ILogger<CsvExportService> _logger;
        private readonly AppSettings _settings;

        public CsvExportService(
            TMRRadzenContext dbContext,
            ILogger<CsvExportService> logger,
            IOptions<AppSettings> settings)
        {
            _dbContext = dbContext;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<string> ExportAssuranceCsvAsync(string timestamp, CancellationToken ct = default)
        {
            // Create output directory
            Directory.CreateDirectory(_settings.OutputDirectory);
            string filePath = Path.Combine(_settings.OutputDirectory, $"assurance-report-{timestamp}.csv");

            // Set command timeout
            _dbContext.Database.SetCommandTimeout(_settings.SqlCommandTimeoutSec);

            var query = BuildAssuranceQuery()
                .AsNoTracking()
                .Take(_settings.CsvMaxRows);

            _logger.LogInformation("Starting CSV export with max {MaxRows} rows", _settings.CsvMaxRows);

            await WriteCsvFileAsync(filePath, query, ct);

            _logger.LogInformation("CSV export complete. File: {FilePath}", Path.GetFullPath(filePath));
            return filePath;
        }

        private IQueryable<AssuranceCsvRow> BuildAssuranceQuery()
        {
            // Structured branch
            var structured =
                from a in _dbContext.AssuranceSubmissionProcesseds
                join d in _dbContext.Divisions on a.DivisionId equals d.Id
                join u in _dbContext.AspNetUsers on a.CreatedById equals u.Id
                join t in _dbContext.AssuranceTemplates on a.AssuranceTemplateId equals t.Id
                join al in _dbContext.AssuranceSubmissionLogs on a.AssuranceSubmissionLogId equals al.Id
                join ap in _dbContext.AssurancePrograms on al.AssuranceProgramId equals ap.Id
                join s in _dbContext.AssuranceSubmissionProcessedResponsesStructureds
                    on a.Id equals s.AssuranceSubmissionProcessedId

                // LEFT JOINs
                join sa0 in _dbContext.AssuranceSubmissionprocessedResponsesStructuredAnswers
                    on s.Id equals sa0.AssuranceSubmissionProcessedResponsesStructuredId into saGrp
                from sa in saGrp.DefaultIfEmpty()

                join c0 in _dbContext.AssuranceSubmissionProcessedComments
                    on a.Id equals c0.AssuranceSubmissionProcessedId into cGrp
                from c in cGrp.DefaultIfEmpty()

                join w0 in _dbContext.AssuranceSubmissionProcessedRisksStandardsActsWeightings
                    on a.Id equals w0.AssuranceSubmissionprocessedId into wGrp
                from w in wGrp.DefaultIfEmpty()

                select new AssuranceCsvRow
                {
                    A_Id = a.Id.ToString(),
                    Location = d.Name,
                    UserName = u.UserName,
                    AssuranceTemplate = t.Name,
                    AssuranceProgram = ap.Name,
                    ProcessedYear = a.CreatedDt.Year,
                    ProcessedMonth = a.CreatedDt.Month,
                    ProcessedDate = a.CreatedDt.Month + "/" + a.CreatedDt.Year,
                    QuestionText = s.QuestionText,
                    QuestionIdentifier = s.QuestionIdentifier,
                    Answer = sa != null ? sa.Answer : null,
                    AnswerBGColor = sa != null ? sa.AnswerBGColor : null,
                    AnswerFGColor = sa != null ? sa.AnswerFGColor : null,
                    Comment = c != null ? c.Comment : null,
                    RiskStandardActName = _dbContext.RisksStandardsActs
                        .Where(x => w != null && x.Id == w.RisksStandardsActsId)
                        .Select(x => x.Name)
                        .FirstOrDefault()
                };

            return structured;
        }

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

        private static string ConvertToString(object? value) =>
            value switch
            {
                null => string.Empty,
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                bool b => b ? "true" : "false",
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };

        private static string EscapeCsv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            bool mustQuote = s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            return mustQuote ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
        }
    }
}