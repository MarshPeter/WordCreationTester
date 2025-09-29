using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CsvParser.Configuration;

namespace CsvParser.Services
{
    public class CsvDuplicateRemovalService
    {
        private readonly ILogger<CsvDuplicateRemovalService> _logger;
        private readonly AIConfig _settings;

        public CsvDuplicateRemovalService(
            ILogger<CsvDuplicateRemovalService> logger,
            IOptions<AIConfig> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

       
        // Remove duplicates from a CSV. A row is a duplicate iff its FULL set of column values
        // (after normalization) matches a previous row (if keepFirst) or any row (if keepLast).
      
        public async Task<string> RemoveDuplicatesAsync(
            string inputFilePath,
            string? outputFilePath = null,
            bool keepFirst = true,
            CancellationToken ct = default)
        {
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException($"Input file not found: {inputFilePath}");

            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                var directory = Path.GetDirectoryName(inputFilePath) ?? _settings.OutputDirectory;
                var fileName = Path.GetFileNameWithoutExtension(inputFilePath);
                var extension = Path.GetExtension(inputFilePath);
                outputFilePath = Path.Combine(directory, $"{fileName}_deduplicated{extension}");
            }

            using var reader = new StreamReader(inputFilePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var headerRow = await reader.ReadLineAsync() ?? "";
            if (string.IsNullOrEmpty(headerRow))
                throw new InvalidDataException("CSV file appears to be empty or missing header");

            var delimiter = DetectDelimiter(headerRow);

            int totalRows = 0, duplicateRows = 0, writtenRows = 0;
            var logEvery = Math.Max(1, _settings.CsvLogEvery);

            if (keepFirst)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                using var writer = new StreamWriter(outputFilePath!, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                await writer.WriteLineAsync(headerRow);

                while (!reader.EndOfStream)
                {
                    ct.ThrowIfCancellationRequested();
                    var raw = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(raw)) continue;

                    totalRows++;
                    var cols = ParseCsvRow(raw, delimiter);
                    var key = BuildNormalizedKeyAllColumns(cols);

                    if (seen.Add(key))
                    {
                        await writer.WriteLineAsync(raw);
                        writtenRows++;
                    }
                    else
                    {
                        duplicateRows++;
                    }

                    if (totalRows % logEvery == 0)
                        _logger.LogInformation("Processed {N}, wrote {W}, dups {D}", totalRows, writtenRows, duplicateRows);
                }

                await writer.FlushAsync();
            }
            else
            {
                // keep last occurrence
                var lastByKey = new Dictionary<string, string>(StringComparer.Ordinal);
                var orderSeen = new Dictionary<string, int>(StringComparer.Ordinal);

                while (!reader.EndOfStream)
                {
                    ct.ThrowIfCancellationRequested();
                    var raw = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(raw)) continue;

                    totalRows++;
                    var cols = ParseCsvRow(raw, delimiter);
                    var key = BuildNormalizedKeyAllColumns(cols);

                    if (!lastByKey.ContainsKey(key))
                        orderSeen[key] = totalRows;
                    else
                        duplicateRows++;

                    lastByKey[key] = raw;

                    if (totalRows % logEvery == 0)
                        _logger.LogInformation("Processed {N}, unique {U}, dups {D}", totalRows, lastByKey.Count, duplicateRows);
                }

                using var writer = new StreamWriter(outputFilePath!, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                await writer.WriteLineAsync(headerRow);

                foreach (var key in orderSeen.OrderBy(kv => kv.Value).Select(kv => kv.Key))
                {
                    await writer.WriteLineAsync(lastByKey[key]);
                    writtenRows++;
                    if (writtenRows % logEvery == 0) await writer.FlushAsync();
                }

                await writer.FlushAsync();
            }

            _logger.LogInformation("Done. Input {In}, Output {Out}, Removed {Dup}. File: {File}",
                totalRows, writtenRows, duplicateRows, Path.GetFullPath(outputFilePath!));

            return outputFilePath!;
        }

    
        // Analyze duplicates using ALL columns as the key (after normalization).
     
        public async Task<DuplicateAnalysisResult> AnalyzeDuplicatesAsync(
            string filePath,
            CancellationToken ct = default)
        {
            var result = new DuplicateAnalysisResult();
            var groups = new Dictionary<string, List<int>>(StringComparer.Ordinal);

            using var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var header = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(header)) return result;

            var delimiter = DetectDelimiter(header);

            int rowNum = 0;
            while (!reader.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                rowNum++;
                var cols = ParseCsvRow(line, delimiter);
                var key = BuildNormalizedKeyAllColumns(cols);

                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    groups[key] = list;
                }
                list.Add(rowNum);
            }

            result.TotalRows = rowNum;
            result.UniqueKeys = groups.Count;
            result.DuplicateGroups = groups.Where(kvp => kvp.Value.Count > 1)
                                           .ToDictionary(k => k.Key, v => v.Value);
            result.TotalDuplicates = result.DuplicateGroups.Values.Sum(v => v.Count - 1);
            return result;
        }

        // ---------------- helpers: delimiter, parse, normalization ----------------

        private static char DetectDelimiter(string headerLine)
        {
            var candidates = new[] { ',', ';', '\t' };
            var counts = candidates.ToDictionary(c => c, c => 0);
            bool inQuotes = false;

            for (int i = 0; i < headerLine.Length; i++)
            {
                var ch = headerLine[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < headerLine.Length && headerLine[i + 1] == '"') { i++; continue; }
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes && counts.ContainsKey(ch))
                {
                    counts[ch]++;
                }
            }
            var best = counts.OrderByDescending(kv => kv.Value).FirstOrDefault();
            return best.Value > 0 ? best.Key : ',';
        }

        private static string[] ParseCsvRow(string row, char delimiter)
        {
            var result = new List<string>(16);
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < row.Length; i++)
            {
                var c = row[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < row.Length && row[i + 1] == '"')
                    {
                        sb.Append('"'); i++;
                    }
                    else inQuotes = !inQuotes;
                }
                else if (c == delimiter && !inQuotes)
                {
                    result.Add(sb.ToString()); sb.Clear();
                }
                else sb.Append(c);
            }
            result.Add(sb.ToString());
            return result.ToArray();
        }

        private static readonly Regex MultiSpace = new Regex(@"\s+", RegexOptions.Compiled);

        private static string NormalizeForKey(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var trimmed = value.Trim();
            var collapsed = MultiSpace.Replace(trimmed, " ");
            var lower = collapsed.ToLowerInvariant();

            // strip accents
            var normalized = lower.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string BuildNormalizedKeyAllColumns(string[] columns)
        {
            // Use ALL columns
            for (int i = 0; i < columns.Length; i++)
                columns[i] = NormalizeForKey(columns[i]);

            return string.Join("|", columns);
        }
    }

    public class DuplicateAnalysisResult
    {
        public int TotalRows { get; set; }
        public int UniqueKeys { get; set; }
        public int TotalDuplicates { get; set; }
        public Dictionary<string, List<int>> DuplicateGroups { get; set; } = new();
        public double DuplicationRate => TotalRows > 0 ? (double)TotalDuplicates / TotalRows * 100 : 0;
    }
}
