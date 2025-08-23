using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; 
using WordCreationTester.Data;       

namespace WordCreationTester
{
    public static class CsvReportExporterEfLinq
    {
      
        public static async Task<string> ExportAssuranceCsvLinqAsync(string timestamp, CancellationToken ct = default)
        {
            // Tunables (env optional)
            int maxRows = ReadIntEnv("CSV_MAX_ROWS", 50000);
            int logEvery = ReadIntEnv("CSV_LOG_EVERY", 5000);
            int maxFieldChars = ReadIntEnv("CSV_MAX_FIELD_CHARS", 2000);
            int cmdTimeoutSec = ReadIntEnv("SQL_COMMAND_TIMEOUT_SEC", 600);

            // Known output folder
            string outDir = Path.Combine("docs", "temp_csv");
            Directory.CreateDirectory(outDir);
            string filePath = Path.Combine(outDir, $"assurance-report-{timestamp}.csv");

            // Use your scaffolded DbContext. If you configured OnConfiguring to read an env var,
            // this parameterless ctor will pick it up.
            var conn = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException("Missing env var SQL_CONNECTION_STRING");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(conn)
                .EnableDetailedErrors()
                // .EnableSensitiveDataLogging()  (optional while debugging)
                .Options;

            using var db = new AppDbContext(options);
            db.Database.SetCommandTimeout(cmdTimeoutSec);

            // ---------------- Structured branch ----------------
            // NOTE: DbSet names and property names must match YOUR scaffold.
            // If any name below doesn't compile, open Data/AppDbContext.cs and copy
            // the exact DbSet property names here.
            var structured =
                from a in db.AssuranceSubmissionProcessed
                join d in db.Divisions on a.DivisionId equals d.Id
                join u in db.AspNetUsers on a.CreatedById equals u.Id
                join t in db.AssuranceTemplates on a.AssuranceTemplateId equals t.Id
                join al in db.AssuranceSubmissionLog on a.AssuranceSubmissionLogId equals al.Id
                join ap in db.AssurancePrograms on al.AssuranceProgramId equals ap.Id
                join s in db.AssuranceSubmissionProcessedResponsesStructured
                                                                    on a.Id equals s.AssuranceSubmissionProcessedId

                // LEFT JOIN answers (structured)
                join sa0 in db.AssuranceSubmissionprocessedResponsesStructuredAnswers
                       on s.Id equals sa0.AssuranceSubmissionProcessedResponsesStructuredId into saGrp
                from sa in saGrp.DefaultIfEmpty()

                    // LEFT JOIN comments
                join c0 in db.AssuranceSubmissionProcessedComments
                       on a.Id equals c0.AssuranceSubmissionProcessedId into cGrp
                from c in cGrp.DefaultIfEmpty()

                    // LEFT JOIN weightings
                join w0 in db.AssuranceSubmissionProcessedRisksStandardsActsWeightings
                       on a.Id equals w0.AssuranceSubmissionprocessedId into wGrp
                from w in wGrp.DefaultIfEmpty()

                select new AssuranceCsvRow
                {
                    A_Id = a.Id.ToString(),
                    Location = d.Name,
                    UserName = u.UserName, // adjust to Username if your scaffold uses that
                    AssuranceTemplate = t.Name,
                    AssuranceProgram = ap.Name,
                    // CreatedDt is non-nullable DateTime in your scaffold, so read directly:
                    ProcessedYear = a.CreatedDt.Year,
                    ProcessedMonth = a.CreatedDt.Month,
                    ProcessedDate = a.CreatedDt.Month + "/" + a.CreatedDt.Year,

                    QuestionText = s.QuestionText,
                    QuestionIdentifier = s.QuestionIdentifier,
                    Answer = sa != null ? sa.Answer : null,
                    AnswerBGColor = sa != null ? sa.AnswerBGColor : null,
                    AnswerFGColor = sa != null ? sa.AnswerFGColor : null,
                    Comment = c != null ? c.Comment : null,

                    // Correlated subquery avoids null-join-key issues and translates well
                    RiskStandardActName = db.RisksStandardsActs
                        .Where(x => w != null && x.Id == w.RisksStandardsActsId)
                        .Select(x => x.Name)
                        .FirstOrDefault(),
                };


            var query = structured
                //.Concat(unstructured)
                .AsNoTracking()
                .Take(maxRows);

            Console.WriteLine($"[EF-LINQ CSV] Starting export (max {maxRows:N0} rows) ...");

            // Write CSV (UTF-8 BOM so Excel opens cleanly)
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16);
            using var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            // Header
            var props = typeof(AssuranceCsvRow).GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                if (i > 0) sw.Write(',');
                sw.Write(EscapeCsv(props[i].Name));
            }
            await sw.WriteLineAsync();

            // Rows
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
                        if (s.Length > maxFieldChars) s = s.Substring(0, maxFieldChars) + "…";
                        sw.Write(EscapeCsv(s));
                    }
                    await sw.WriteLineAsync();

                    written++;
                    if (written % logEvery == 0)
                    {
                        await sw.FlushAsync();
                        Console.WriteLine($"[EF-LINQ CSV] Wrote {written:N0} rows...");
                    }
                }
            }
            catch (Exception ex)
            {
                // If EF cannot translate something, this will show the exact reason.
                Console.WriteLine("[EF-LINQ CSV] ERROR:\n" + ex.ToString());
                throw;
            }

            await sw.FlushAsync();
            Console.WriteLine($"[EF-LINQ CSV] Complete. Rows written: {written:N0}");
            Console.WriteLine($"[EF-LINQ CSV] Local file: {Path.GetFullPath(filePath)}");

            return filePath;
        }

        // ---------- helpers ----------
        private static string ConvertToString(object value) =>
            value switch
            {
                null => string.Empty,
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                bool b => b ? "true" : "false",
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };

        private static string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            bool mustQuote = s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            return mustQuote ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
        }

        private static int ReadIntEnv(string name, int fallback)
        {
            var v = Environment.GetEnvironmentVariable(name);
            return int.TryParse(v, out var n) ? n : fallback;
        }
    }

    
}
