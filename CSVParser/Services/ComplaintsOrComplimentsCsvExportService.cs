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
    public class ComplaintsOrComplimentsCsvExportService  : ICSVExporter
    {
        private readonly TMRRadzenContext _dbContext;
        private readonly ILogger<ComplaintsOrComplimentsCsvExportService> _logger;
        private readonly AIConfig _settings;

        public ComplaintsOrComplimentsCsvExportService(
            TMRRadzenContext dbContext,
            ILogger<ComplaintsOrComplimentsCsvExportService> logger,
            IOptions<AIConfig> settings)
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

            var query = BuildComplaintsOrComplimentsQuery()
                .AsNoTracking()
                .Take(_settings.CsvMaxRows); // Max rows set to prevent AISearch Index Limits from being breached.

            await WriteCsvFileAsync(filePath, query, ct);

            _logger.LogInformation("CSV export complete. File: {FilePath}", Path.GetFullPath(filePath));
            return filePath;
        }

        private IQueryable<ComplaintsOrComplimentsCsvRow> BuildComplaintsOrComplimentsQuery()
        {
            var query = from cc in _dbContext.ComplaintsOrCompliments
                            // LEFT JOIN for comments
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

            return query;
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