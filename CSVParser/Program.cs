using CsvParser.Configuration;
using CsvParser.DTO;
using CsvParser.Services;
using CSVParser.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CsvParser
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // Read as nullable, then validate
            string? tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                var bootLogger = host.Services.GetRequiredService<ILogger<Program>>();
                bootLogger.LogError("Required environment variable AZURE_TENANT_ID is not set.");
                Environment.Exit(1);
                return; 
            }

            try
            {
                // We should have less indexes in this list than the number that is allowed in AI Search Services and our Tier
                // For basic tier, we must not have more than 15
                var indexes = new List<IndexDefinition>
                {
                    new IndexDefinition(
                        "assurances",
                        "Assurances",
                        "Contains informtion regarding comments made regarding assurance practices",
                        host.Services.GetRequiredService<AssuranceCsvExportService>()),

                    new IndexDefinition(
                        "issues-actions-tasks",
                        "Issues/Actions/Tasks",
                        "Contains information about issues that have been listed, actions that have been made, and tasks that have been set to monitor the issues",
                        host.Services.GetRequiredService<IssuesActionTasksCsvExportService>()),

                    new IndexDefinition(
                        "complaints-and-complements",
                        "Complaints And Complements",
                        "Contains information about received complaints and complements towards the business",
                        host.Services.GetRequiredService<ComplaintsOrComplimentsCsvExportService>())
                };


                var azureUploadService = host.Services.GetRequiredService<AzureUploadService>();
                var duplicateRemovalService = host.Services.GetRequiredService<CsvDuplicateRemovalService>();
                var indexer = host.Services.GetRequiredService<IndexCreatorService>();
                var logger = host.Services.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Starting CSV export process for {tenant_Id}...", tenantId);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

               
                // it goes through each index and make CSV clean it up by removing  duplicates, 
                //upload the finished file to Azure, then delete the local copies so nothing is left behind. 
                foreach (var idx in indexes)
                {
                    string csvPath = await idx.ExportService.ExportCSV(idx.IndexName, timestamp);
                    logger.LogInformation($"{idx.IndexName} CSV created at: {csvPath}");

                   // remove duplicate files from CSV
                    string finalCsvPath = await ProcessCsvForDuplicates(
                        csvPath,
                        duplicateRemovalService,
                        idx.IndexName,
                        logger
                    );
                    logger.LogInformation("Duplicates removed from {index_name}", idx.IndexName);

                    //upload cleaned CSV to Azure
                    string assuranceBlobName = Path.GetFileName(finalCsvPath);
                    await azureUploadService.UploadFileAsync(finalCsvPath, assuranceBlobName, idx.IndexName);
                    logger.LogInformation("{index_name} CSV uploaded as: {assurance_blob_name}", idx.IndexName, assuranceBlobName);

                  // delete temporary local files
                    File.Delete(finalCsvPath);
                    File.Delete(csvPath);
                    logger.LogInformation("Local leftover CSV files have been removed");
                }

                //update index info in the database
                await indexer.UpdateDatabaseIndexInformation(indexes, tenantId);

                logger.LogInformation("Index creation for {tenant_id} has completed succesfully", tenantId);

            }
            catch (Exception ex)
            {
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred during CSV processing for {tenant_id}", tenantId);
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
                    services.Configure<AIConfig>(context.Configuration.GetSection("AIConfig"));

                    // Database
                    services.AddDbContext<TMRRadzenContext>(options =>
                        options.UseSqlServer(Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING"))); // This needs to be set here regardless of AIConfig.cs setup


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