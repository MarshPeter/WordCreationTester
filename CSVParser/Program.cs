using CsvParser.Configuration;
using CsvParser.DTO;
using CsvParser.Services;
using CSVParser.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CsvParser.QueryTemplates;

namespace CsvParser
{
    class Program
    {

        static async Task Main(string[] args)
        {

            var host = CreateHostBuilder(args).Build();

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

                // Get generic services
                var indexSeeder = host.Services.GetRequiredService<IndexSeedingService>();
                var csvExporter = host.Services.GetRequiredService<CsvExportService>();
                var duplicateRemover = host.Services.GetRequiredService<CsvDuplicateRemovalService>();
                var csvSplitter = host.Services.GetRequiredService<CsvSplitterService>();
                var azureUploader = host.Services.GetRequiredService<CsvAzureUploadService>();
                var indexer = host.Services.GetRequiredService<IndexCreatorService>();
                var logger = host.Services.GetRequiredService<ILogger<Program>>();


                // TODO: Need to finalize how the seed list is supposed to map to the index list
                // Simulating a seed List
                var seeds = new List<string> { "assurances", "issues-actions-tasks", "complaints-and-complements" };

                var indexes = indexSeeder.GetTenantSeededIndexes(seeds);
                var notTenantIndexes = indexSeeder.GetNonTenantSeededIndexes(seeds);

                logger.LogInformation("Starting CSV export process for {TenantId}...", tenantId);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                if (indexes.Count > 15)
                {
                    logger.LogError("Too many indexes ({Count}). Max 15 allowed.", indexes.Count);
                    return;
                }

                // Process each index using generic services
                foreach (var idx in indexes)
                {
                    logger.LogInformation("========== Processing: {IndexName} ==========", idx.IndexName);

                    // Step 1: Build query using reflection
                    var queryService = host.Services.GetRequiredService(idx.QueryServiceType);
                    var getQueryMethod = idx.QueryServiceType.GetMethod("GetQuery");
                    var query = getQueryMethod!.Invoke(queryService, null);

                    // Step 2: Export to CSV using generic exporter
                    string csvPath = Path.Combine(
                        host.Services.GetRequiredService<IOptions<AIConfig>>().Value.OutputDirectory,
                        $"{idx.IndexName}-{timestamp}.csv");

                    var exportMethod = typeof(CsvExportService)
                        .GetMethod("ExportToCSV")!
                        .MakeGenericMethod(idx.RowType);

                    csvPath = (string)(await (Task<string>)exportMethod.Invoke(
                        csvExporter,
                        new[] { query, csvPath, CancellationToken.None })!)!;

                    logger.LogInformation("✓ Exported to: {CsvPath}", csvPath);

                    // Track the original export file for cleanup
                    string originalCsvPath = csvPath;

                    // Step 3: Deduplicate using generic service
                    csvPath = await ProcessDuplicates(csvPath, duplicateRemover, logger);
                    logger.LogInformation("✓ Deduplication complete");

                    // Step 4: Split files up if they are too large using generic service
                    var filePaths = await csvSplitter.SplitCsvFileAsync(csvPath);
                    logger.LogInformation("✓ Split into {Count} file(s)", filePaths.Count);

                    // Step 5: Upload using generic service
                    await azureUploader.ClearIndexFolderAsync(idx.IndexName);
                    foreach (var file in filePaths)
                    {
                        await azureUploader.UploadFileAsync(file, Path.GetFileName(file), idx.IndexName);
                        logger.LogInformation("Uploaded: {FileName}", Path.GetFileName(file));
                    }
                    logger.LogInformation("✓ Uploaded {Count} file(s)", filePaths.Count);

                    // Step 6: Cleanup ALL temporary files
                    var filesToDelete = new List<string>();

                    // Add original export file
                    filesToDelete.Add(originalCsvPath);

                    // Add all split files
                    filesToDelete.AddRange(filePaths);

                    foreach (var file in filesToDelete.Distinct())
                    {
                        try
                        {
                            if (File.Exists(file))
                            {
                                // Retry logic for file locks
                                int retries = 3;
                                bool deleted = false;

                                while (retries > 0 && !deleted)
                                {
                                    try
                                    {
                                        File.Delete(file);
                                        logger.LogInformation("  Deleted: {FileName}", Path.GetFileName(file));
                                        deleted = true;
                                    }
                                    catch (IOException) when (retries > 1)
                                    {
                                        retries--;
                                        await Task.Delay(200); // Wait 200ms before retry
                                    }
                                }

                                if (!deleted)
                                {
                                    logger.LogWarning("Could not delete file after retries: {FileName}", Path.GetFileName(file));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Cleanup failed: {File}", file);
                        }
                    }
                    logger.LogInformation("✓ Cleanup complete\n");
                }
                // Update the database, but also clean up any indexes that are being removed from tenant. 
                // Returns a list of tenants that are being removed, so that we can clean up their relevant containers. 
                List<string> indexesToCleanUp = await indexer.UpdateDatabaseIndexInformation(indexes, notTenantIndexes, tenantId);

                foreach (string index in indexesToCleanUp)
                {
                    await azureUploader.ClearIndexFolderAsync(index);
                }
                
                logger.LogInformation("✓✓✓ All processing complete for {TenantId} ✓✓✓", tenantId);
            }
            catch (Exception ex)
            {
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Error during processing for {TenantId}", tenantId);
                Environment.Exit(1);
            }
        }

        // Analyzes and removes duplicate rows from CSV file, replacing original if duplicates found
        private static async Task<string> ProcessDuplicates(
            string csvPath,
            CsvDuplicateRemovalService remover,
            ILogger logger)
        {
            var analysis = await remover.AnalyzeDuplicatesAsync(csvPath);
            logger.LogInformation(
                "  Analysis - Rows: {Total}, Duplicates: {Dupes} ({Rate:F2}%)",
                analysis.TotalRows, analysis.TotalDuplicates, analysis.DuplicationRate);

            if (analysis.TotalDuplicates > 0)
            {
                var dedupPath = await remover.RemoveDuplicatesAsync(csvPath, null, true);

                // Wait a bit for file handles to be released
                await Task.Delay(100);

                try
                {
                    File.Delete(csvPath);
                    File.Move(dedupPath, csvPath);
                }
                catch (IOException ex)
                {
                    logger.LogWarning(ex, "Could not replace original file, using deduplicated file directly");
                    return dedupPath; // Return the deduplicated file if we can't replace
                }
            }

            return csvPath;
        }

        // Configures dependency injection and registers all services
        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.Configure<AIConfig>(context.Configuration.GetSection("AIConfig"));

                    services.AddDbContext<TMRRadzenContext>(options =>
                        options.UseSqlServer(Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")));

                    // Generic services
                    services.AddScoped<CsvExportService>();
                    services.AddScoped<CsvDuplicateRemovalService>();
                    services.AddScoped<CsvSplitterService>();
                    services.AddScoped<CsvAzureUploadService>();
                    services.AddScoped<IndexCreatorService>();
                    services.AddScoped<IndexSeedingService>();

                    // Query services
                    services.AddScoped<AssuranceQueryService>();
                    services.AddScoped<IssuesActionTasksQueryService>();
                    services.AddScoped<ComplaintsOrComplimentsQueryService>();
                });
    }
}