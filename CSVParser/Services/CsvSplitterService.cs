using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CsvParser.Configuration;

namespace CsvParser.Services
{
    public class CsvSplitterService
    {
        private const long MAX_FILE_SIZE_BYTES = 12 * 1024 * 1024; // 12 MB
        private const int SIZE_CHECK_INTERVAL = 5000; // Check size every N rows

        private readonly ILogger<CsvSplitterService> _logger;
        private readonly AIConfig _settings;

        public CsvSplitterService(
            ILogger<CsvSplitterService> logger,
            IOptions<AIConfig> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        // Splits a large CSV file into multiple smaller files at the 12MB mark
        public async Task<List<string>> SplitCsvFileAsync(
            string inputFilePath,
            CancellationToken ct = default)
        {
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException($"Input file not found: {inputFilePath}");

            var outputFiles = new List<string>();
            var fileInfo = new FileInfo(inputFilePath);

            // If file is under 12MB, no need to split
            if (fileInfo.Length < MAX_FILE_SIZE_BYTES)
            {
                _logger.LogInformation(
                    "File {FileName} is {SizeMB:F2} MB, no splitting needed",
                    Path.GetFileName(inputFilePath),
                    fileInfo.Length / (1024.0 * 1024.0));
                outputFiles.Add(inputFilePath);
                return outputFiles;
            }

            _logger.LogInformation(
                "Splitting large file {FileName} ({SizeMB:F2} MB) into smaller chunks...",
                Path.GetFileName(inputFilePath),
                fileInfo.Length / (1024.0 * 1024.0));

            string? headerLine = null;
            int partNumber = 1;
            long rowsInCurrentFile = 0;
            long totalRowsProcessed = 0;
            StreamWriter? currentWriter = null;
            FileStream? currentStream = null;

            try
            {
                using var reader = new StreamReader(inputFilePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

                // Read and store header
                headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(headerLine))
                    throw new InvalidDataException("CSV file appears to be empty or missing header");

                // CHANGED: Always create part files with different names (never reuse input filename)
                var currentFilePath = GetPartFilePath(inputFilePath, partNumber, alwaysAddSuffix: true);
                (currentStream, currentWriter) = await StartNewPartFileAsync(currentFilePath, headerLine);
                outputFiles.Add(currentFilePath);

                // Process each data row
                while (!reader.EndOfStream)
                {
                    ct.ThrowIfCancellationRequested();

                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;

                    await currentWriter.WriteLineAsync(line);
                    rowsInCurrentFile++;
                    totalRowsProcessed++;

                    // Check file size periodically
                    if (rowsInCurrentFile % SIZE_CHECK_INTERVAL == 0)
                    {
                        await currentWriter.FlushAsync();

                        // Check if current file exceeds size limit
                        if (currentStream.Length >= MAX_FILE_SIZE_BYTES)
                        {
                            _logger.LogInformation(
                                "Part {PartNumber} reached {SizeMB:F2} MB with {Rows} rows. Starting new part...",
                                partNumber,
                                currentStream.Length / (1024.0 * 1024.0),
                                rowsInCurrentFile);

                            // Close current file
                            await currentWriter.FlushAsync();
                            await currentWriter.DisposeAsync();
                            await currentStream.DisposeAsync();
                            currentWriter = null;
                            currentStream = null;

                            // Start new part file
                            partNumber++;
                            rowsInCurrentFile = 0;
                            currentFilePath = GetPartFilePath(inputFilePath, partNumber, alwaysAddSuffix: true);
                            (currentStream, currentWriter) = await StartNewPartFileAsync(currentFilePath, headerLine);
                            outputFiles.Add(currentFilePath);
                        }
                    }
                }

                // Final flush
                if (currentWriter != null)
                {
                    await currentWriter.FlushAsync();
                }

                _logger.LogInformation(
                    "Split complete. Created {FileCount} file(s) with {TotalRows} total rows",
                    outputFiles.Count,
                    totalRowsProcessed);

                return outputFiles;
            }
            finally
            {
                // Ensure all streams are disposed
                if (currentWriter != null)
                {
                    await currentWriter.DisposeAsync();
                }
                if (currentStream != null)
                {
                    await currentStream.DisposeAsync();
                }
            }

            // NOTE: Original input file is NOT deleted - let Program.cs handle cleanup
        }

        private static async Task<(FileStream, StreamWriter)> StartNewPartFileAsync(
            string filePath,
            string headerLine)
        {
            var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16);
            var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            // Write header
            await sw.WriteLineAsync(headerLine);

            return (fs, sw);
        }

        private static string GetPartFilePath(string baseFilePath, int partNumber, bool alwaysAddSuffix = false)
        {
            var directory = Path.GetDirectoryName(baseFilePath);
            var fileName = Path.GetFileNameWithoutExtension(baseFilePath);
            var extension = Path.GetExtension(baseFilePath);

            // CHANGED: Always add suffix if requested (to avoid filename conflicts)
            if (alwaysAddSuffix)
            {
                return Path.Combine(directory!, $"{fileName}-part{partNumber}{extension}");
            }
            else
            {
                // Legacy behavior: first file keeps original name
                if (partNumber == 1)
                    return baseFilePath;

                return Path.Combine(directory!, $"{fileName}-part{partNumber}{extension}");
            }
        }
    }
}