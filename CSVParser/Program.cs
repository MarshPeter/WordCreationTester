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
                var csvExportService = host.Services.GetRequiredService<CsvExportService>();
                var azureUploadService = host.Services.GetRequiredService<AzureUploadService>();
                var indexer = host.Services.GetRequiredService<IndexCreatorService>();
                var logger = host.Services.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Starting CSV export process...");

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // bool res = await indexer.UpdateDatabaseIndexInformation(test, tenantId);

                // Export CSV
                string csvPath = await csvExportService.ExportAssuranceCsvAsync(timestamp);
                logger.LogInformation($"CSV exported to: {csvPath}");

                // Upload to Azure
                string blobName = Path.GetFileName(csvPath);
                await azureUploadService.UploadFileAsync(csvPath, blobName);
                logger.LogInformation($"CSV uploaded as: {blobName}");

                // Clean up local file
                File.Delete(csvPath);

                logger.LogInformation("Local CSV file cleaned up");
            }
            catch (Exception ex)
            {
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred during CSV processing");
                throw;
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Configuration
                    services.Configure<AppSettings>(context.Configuration);

                    // Database
                    services.AddDbContext<TMRRadzenContext>(options =>
                        options.UseSqlServer(Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")));

                    // Services
                    services.AddScoped<CsvExportService>();
                    services.AddScoped<AzureUploadService>();
                    services.AddScoped<IndexCreatorService>();
                });
    }
}
