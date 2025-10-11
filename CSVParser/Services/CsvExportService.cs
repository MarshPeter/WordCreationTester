using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CsvParser.Configuration;

namespace CsvParser.Services
{
    public class CsvExportService
    {
        private readonly ILogger<CsvExportService> _logger;
        private readonly AIConfig _settings;

        // Exports database query results to CSV files with configurable field truncation
        public CsvExportService(
            ILogger<CsvExportService> logger,
            IOptions<AIConfig> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        // Exports an any IQueryable to a CSV file with proper escaping and formatting

        public async Task<string> ExportToCSV<T>(
            IQueryable<T> query,
            string outputFilePath,
            CancellationToken ct = default)
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var fs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16);
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
                _logger.LogError(ex, "Error during CSV export to {FilePath}", outputFilePath);
                throw;
            }

            await sw.FlushAsync();
            _logger.LogInformation(
                "CSV export complete. File: {FilePath}, Total rows: {TotalRows}",
                Path.GetFullPath(outputFilePath),
                written);

            return outputFilePath;
        }

        // Converts various data types to string with proper formatting
        private static string ConvertToString(object? value) =>
            value switch
            {
                null => string.Empty,
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                bool b => b ? "true" : "false",
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };

        // Escapes special CSV characters by wrapping in quotes and doubling internal quotes
        private static string EscapeCsv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            bool mustQuote = s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            return mustQuote ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
        }
    }
}