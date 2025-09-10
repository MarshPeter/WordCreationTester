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

        public async Task<string> ExportCSV(string csvName, string timestamp, CancellationToken ct = default)
        {
            // Create output directory
            Directory.CreateDirectory(_settings.OutputDirectory);
            string filePath = Path.Combine(_settings.OutputDirectory, $"{csvName}-{timestamp}.csv");

            // Set command timeout
            _dbContext.Database.SetCommandTimeout(_settings.SqlCommandTimeoutSec);

            _logger.LogInformation("Starting CSV export with max {MaxRows} rows", _settings.CsvMaxRows);

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16);
            using var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            // Write header
            await sw.WriteLineAsync("Location,UserName,AssuranceTemplate,AssuranceProgram,ProcessedYear,ProcessedMonth,ProcessedDate,QuestionText,QuestionIdentifier,Answer,AnswerBGColor,AnswerFGColor,Comment,RiskStandardActName");

            // STRUCTURED RESPONSES QUERY - exactly matching your SQL
            var structuredQuery = from a in _dbContext.AssuranceSubmissionProcesseds
                                  join s in _dbContext.AssuranceSubmissionProcessedResponsesStructureds
                                    on a.Id equals s.AssuranceSubmissionProcessedId
                                  join d in _dbContext.Divisions on a.DivisionId equals d.Id
                                  join u in _dbContext.AspNetUsers on a.CreatedById equals u.Id
                                  join atemp in _dbContext.AssuranceTemplates on a.AssuranceTemplateId equals atemp.Id
                                  join al in _dbContext.AssuranceSubmissionLogs on a.AssuranceSubmissionLogId equals al.Id
                                  join ap in _dbContext.AssurancePrograms on al.AssuranceProgramId equals ap.Id

                                  // LEFT JOIN for structured answers
                                  join sa in _dbContext.AssuranceSubmissionprocessedResponsesStructuredAnswers
                                    on s.Id equals sa.AssuranceSubmissionProcessedResponsesStructuredId into saGroup
                                  from sa in saGroup.DefaultIfEmpty()

                                  // LEFT JOIN for comments
                                  join c in _dbContext.AssuranceSubmissionProcessedComments
                                    on a.Id equals c.AssuranceSubmissionProcessedId into cGroup
                                  from c in cGroup.DefaultIfEmpty()

                                  // LEFT JOIN for weightings
                                  join w in _dbContext.AssuranceSubmissionProcessedRisksStandardsActsWeightings
                                    on a.Id equals w.AssuranceSubmissionprocessedId into wGroup
                                  from w in wGroup.DefaultIfEmpty()

                                  // INNER JOIN for risk acts (this filters out records without risk data)
                                  join r in _dbContext.RisksStandardsActs
                                    on w.RisksStandardsActsId equals r.Id

                                  select new
                                  {
                                      Location = d.Name,
                                      UserName = u.UserName,
                                      AssuranceTemplate = atemp.Name,
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
                                      Name = r.Name
                                  };

            // UNSTRUCTURED RESPONSES QUERY - exactly matching your SQL
            var unstructuredQuery = from a in _dbContext.AssuranceSubmissionProcesseds
                                    join s in _dbContext.AssuranceSubmissionProcessedResponsesUnStructureds
                                      on a.Id equals s.AssuranceSubmissionprocessedId
                                    join d in _dbContext.Divisions on a.DivisionId equals d.Id
                                    join u in _dbContext.AspNetUsers on a.CreatedById equals u.Id
                                    join atemp in _dbContext.AssuranceTemplates on a.AssuranceTemplateId equals atemp.Id
                                    join al in _dbContext.AssuranceSubmissionLogs on a.AssuranceSubmissionLogId equals al.Id
                                    join ap in _dbContext.AssurancePrograms on al.AssuranceProgramId equals ap.Id

                                    // LEFT JOIN for unstructured answers
                                    join sa in _dbContext.AssuranceSubmissionProcessedResponsesUnStructuredAnswers
                                      on s.Id equals sa.AssuranceSubmissionProcessedResponsesUnStructuredId into saGroup
                                    from sa in saGroup.DefaultIfEmpty()

                                        // LEFT JOIN for comments
                                    join c in _dbContext.AssuranceSubmissionProcessedComments
                                      on a.Id equals c.AssuranceSubmissionProcessedId into cGroup
                                    from c in cGroup.DefaultIfEmpty()

                                    select new
                                    {
                                        Location = d.Name,
                                        UserName = u.UserName,
                                        AssuranceTemplate = atemp.Name,
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
                                        Name = "" // Empty string for unstructured as per your SQL
                                    };

            // Execute both queries
            var structuredResults = await structuredQuery.AsNoTracking().ToListAsync(ct);
            var unstructuredResults = await unstructuredQuery.AsNoTracking().ToListAsync(ct);

            _logger.LogInformation("Structured results: {Count}, Unstructured results: {Count}",
                structuredResults.Count, unstructuredResults.Count);

            // Combine results (UNION equivalent) and apply limit
            var allResults = structuredResults
                .Concat(unstructuredResults)
                .Take(_settings.CsvMaxRows)
                .ToList();

            _logger.LogInformation("Total combined results: {Count}", allResults.Count);

            long rowCount = 0;
            foreach (var item in allResults)
            {
                ct.ThrowIfCancellationRequested();

                var values = new string[14];
                values[0] = EscapeCsv(item.Location ?? "");
                values[1] = EscapeCsv(item.UserName ?? "");
                values[2] = EscapeCsv(item.AssuranceTemplate ?? "");
                values[3] = EscapeCsv(item.AssuranceProgram ?? "");
                values[4] = item.ProcessedYear.ToString();
                values[5] = item.ProcessedMonth.ToString();
                values[6] = EscapeCsv(item.ProcessedDate?.ToString() ?? "");
                values[7] = EscapeCsv(item.QuestionText ?? "");
                values[8] = EscapeCsv(item.QuestionIdentifier ?? "");
                values[9] = EscapeCsv(item.Answer ?? "");
                values[10] = EscapeCsv(item.AnswerBGColor ?? "");
                values[11] = EscapeCsv(item.AnswerFGColor ?? "");
                values[12] = EscapeCsv(item.Comment ?? "");
                values[13] = EscapeCsv(item.Name ?? "");

                // Apply field length limits
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i].Length > _settings.CsvMaxFieldChars)
                        values[i] = values[i].Substring(0, _settings.CsvMaxFieldChars) + "…";
                }

                await sw.WriteLineAsync(string.Join(",", values));
                rowCount++;

                if (rowCount % _settings.CsvLogEvery == 0)
                {
                    await sw.FlushAsync();
                    _logger.LogInformation("Wrote {RowCount} rows...", rowCount);
                }
            }

            await sw.FlushAsync();
            _logger.LogInformation("CSV export complete. Total rows: {TotalRows}, File: {FilePath}", rowCount, Path.GetFullPath(filePath));
            return filePath;
        }

        private static string EscapeCsv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            bool mustQuote = s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            return mustQuote ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
        }
    }
}