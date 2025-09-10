using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CsvParser.Services;
using CsvParser.Configuration;
using CSVParser.Data;
using CsvParser.DTO;

namespace CsvParser
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            var test = new List<IndexDefintion>
            {
                new IndexDefintion("test", "test is a test")
            };

            string tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");


            try
            {
                var csvExportService = host.Services.GetRequiredService<AssuranceCsvExportService>();
                var issuesExportService = host.Services.GetRequiredService<IssuesActionTasksCsvExportService>();
                var complaintsExportService = host.Services.GetRequiredService<ComplaintsOrComplimentsCsvExportService>();
                var azureUploadService = host.Services.GetRequiredService<AzureUploadService>();
                var duplicateRemovalService = host.Services.GetRequiredService<CsvDuplicateRemovalService>();
                var indexer = host.Services.GetRequiredService<IndexCreatorService>();
                var logger = host.Services.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Starting CSV export process...");

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // bool res = await indexer.UpdateDatabaseIndexInformation(test, tenantId);

                // Export CSV
                string csvPath = await csvExportService.ExportAssuranceCsvAsync(timestamp);
                logger.LogInformation($"CSV exported to: {csvPath}");
                // Export Assurance CSV (existing)
                logger.LogInformation("Starting Assurance CSV export...");
                string assuranceCsvPath = await csvExportService.ExportAssuranceCsvAsync(timestamp);
                logger.LogInformation("Assurance CSV exported to: {FilePath}", assuranceCsvPath);

                // Process Assurance CSV for duplicates
                string finalAssuranceCsvPath = await ProcessCsvForDuplicates(
                    assuranceCsvPath,
                    duplicateRemovalService,
                    "Assurance",
                    logger);

                // Upload Assurance CSV to Azure
                string assuranceBlobName = Path.GetFileName(finalAssuranceCsvPath);
                await azureUploadService.UploadFileAsync(finalAssuranceCsvPath, assuranceBlobName);
                logger.LogInformation("Assurance CSV uploaded as: {BlobName}", assuranceBlobName);

                // Clean up local assurance file
                File.Delete(finalAssuranceCsvPath);
                logger.LogInformation("Local Assurance CSV file cleaned up");

                // Export Issues/Actions/Tasks CSV
                logger.LogInformation("Starting Issues/Actions/Tasks CSV export...");
                string issuesCsvPath = await issuesExportService.ExportIssuesActionTasksCsvAsync(timestamp);
                logger.LogInformation("Issues/Actions/Tasks CSV exported to: {FilePath}", issuesCsvPath);

                // Process Issues CSV for duplicates
                string finalIssuesCsvPath = await ProcessCsvForDuplicates(
                    issuesCsvPath,
                    duplicateRemovalService,
                    "Issues/Actions/Tasks",
                    logger);

                // Upload Issues/Actions/Tasks CSV to Azure
                string issuesBlobName = Path.GetFileName(finalIssuesCsvPath);
                await azureUploadService.UploadFileAsync(finalIssuesCsvPath, issuesBlobName);
                logger.LogInformation("Issues/Actions/Tasks CSV uploaded as: {BlobName}", issuesBlobName);

                // Clean up local issues file
                File.Delete(finalIssuesCsvPath);
                logger.LogInformation("Local Issues/Actions/Tasks CSV file cleaned up");

                // Export Complaints or Compliments CSV
                logger.LogInformation("Starting Complaints or Compliments CSV export...");
                string complaintsCsvPath = await complaintsExportService.ExportComplaintsOrComplimentsCsvAsync(timestamp);
                logger.LogInformation("Complaints or Compliments CSV exported to: {FilePath}", complaintsCsvPath);

                // Process Complaints CSV for duplicates
                string finalComplaintsCsvPath = await ProcessCsvForDuplicates(
                    complaintsCsvPath,
                    duplicateRemovalService,
                    "Complaints or Compliments",
                    logger);

                // Upload Complaints or Compliments CSV to Azure
                string complaintsBlobName = Path.GetFileName(finalComplaintsCsvPath);
                await azureUploadService.UploadFileAsync(finalComplaintsCsvPath, complaintsBlobName);
                logger.LogInformation("Complaints or Compliments CSV uploaded as: {BlobName}", complaintsBlobName);

                // Clean up local complaints file
                File.Delete(finalComplaintsCsvPath);
                logger.LogInformation("Local Complaints or Compliments CSV file cleaned up");

                logger.LogInformation("All CSV export processes completed successfully");
            }
            catch (Exception ex)
            {
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred during CSV processing");
                Environment.Exit(1);
            }
        }


        /// Generic method to process any CSV for duplicates while preserving original filename
       
        private static async Task<string> ProcessCsvForDuplicates(
            string originalCsvPath,
            CsvDuplicateRemovalService duplicateRemovalService,
            string csvType,
            ILogger logger)
        {
            try
            {
                // Analyze duplicates first
                var analysis = await duplicateRemovalService.AnalyzeDuplicatesAsync(originalCsvPath);
                logger.LogInformation("{CsvType} duplicate analysis - Total rows: {TotalRows}, Duplicates: {TotalDuplicates} ({DuplicationRate:F2}%)",
                    csvType, analysis.TotalRows, analysis.TotalDuplicates, analysis.DuplicationRate);

                // Remove duplicates if any were found
                if (analysis.TotalDuplicates > 0)
                {
                    logger.LogInformation("Removing duplicates from {CsvType} CSV...", csvType);

                    // Create a temporary path for the deduplicated file
                    var tempDeduplicatedPath = await duplicateRemovalService.RemoveDuplicatesAsync(originalCsvPath, outputFilePath: null, keepFirst: true);

                    // Replace original file with deduplicated version (preserving original filename)
                    File.Delete(originalCsvPath);
                    File.Move(tempDeduplicatedPath, originalCsvPath);

                    logger.LogInformation("{CsvType} duplicates removed successfully", csvType);
                    return originalCsvPath;
                }
                else
                {
                    logger.LogInformation("No duplicates found in {CsvType} CSV, proceeding with original file", csvType);
                    return originalCsvPath;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error during duplicate removal for {CsvType} CSV, proceeding with original file", csvType);
                return originalCsvPath;
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Configuration
                    services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));

                    // Database
                    services.AddDbContext<TMRRadzenContext>(options =>
                        options.UseSqlServer(Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")));

                    // Services
                    services.AddScoped<AssuranceCsvExportService>();
                    services.AddScoped<IssuesActionTasksCsvExportService>();
                    services.AddScoped<ComplaintsOrComplimentsCsvExportService>();
                    services.AddScoped<AzureUploadService>();
                    services.AddScoped<CsvDuplicateRemovalService>();
                    services.AddScoped<IndexCreatorService>();
                });
    }
}