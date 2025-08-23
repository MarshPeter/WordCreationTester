using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace WordCreationTester
{
    public static class CsvReportExporter
    {
        private const string AssuranceCoreSql = @"SELECT distinct a.*, d.name as location, u.username, atemp.name as AssuranceTemplate, ap.name as AssuranceProgram, year(a.createddt) as processedYear, month(a.createddt) as processedMonth, cast(month(a.createddt) as varchar) + '/' + cast(year(a.createddt) as varchar) as processedDate

,s.QuestionText, s.QuestionIdentifier, sa.Answer, sa.AnswerBGColor, sa.AnswerFGColor, c.comment, r.Name

FROM assurancesubmissionprocessed a

INNER JOIN AssuranceSubmissionProcessedResponsesStructured s on a.id = s.AssuranceSubmissionProcessedId

LEFT JOIN AssuranceSubmissionprocessedResponsesStructuredAnswers sa on s.id = sa.AssuranceSubmissionProcessedResponsesStructuredId

INNER JOIN Divisions d on a.divisionid = d.id

INNER JOIN aspnetusers u on a.createdbyid = u.id

INNER JOIN assurancetemplates atemp on a.assurancetemplateid = atemp.id

INNER JOIN  AssuranceSubmissionLog AL on a.assurancesubmissionlogid = al.id

INNER JOIN AssurancePrograms ap on al.assuranceprogramid = ap.id

left JOIN Assurancesubmissionprocessedcomments c on a.id = c.assurancesubmissionprocessedId

left join AssuranceSubmissionProcessedRisksStandardsActsWeightings w on w.AssuranceSubmissionProcessedId = a.id

inner join RisksStandardsActs r on w.RisksStandardsActsId = r.id

 

 

union

 

SELECT distinct a.*, d.name as location, u.username, atemp.name as AssuranceTemplate, ap.name as AssuranceProgram, year(a.createddt) as processedYear, month(a.createddt) as processedMonth, cast(month(a.createddt) as varchar) + '/' + cast(year(a.createddt) as varchar) as processedDate

,s.QuestionText, s.QuestionIdentifier, sa.Answer, sa.AnswerBGColor, sa.AnswerFGColor, c.comment, '' as name

FROM assurancesubmissionprocessed a

INNER JOIN AssuranceSubmissionProcessedResponsesunStructured s on a.id = s.AssuranceSubmissionProcessedId

LEFT JOIN AssuranceSubmissionprocessedResponsesunStructuredAnswers sa on s.id = sa.AssuranceSubmissionProcessedResponsesunStructuredId

INNER JOIN Divisions d on a.divisionid = d.id

INNER JOIN aspnetusers u on a.createdbyid = u.id

INNER JOIN assurancetemplates atemp on a.assurancetemplateid = atemp.id

INNER JOIN  AssuranceSubmissionLog AL on a.assurancesubmissionlogid = al.id

INNER JOIN AssurancePrograms ap on al.assuranceprogramid = ap.id

left JOIN Assurancesubmissionprocessedcomments c on a.id = c.assurancesubmissionprocessedId";

        public static async Task<string> ExportAssuranceCsvToKnownTempPathAsync(string timestamp, CancellationToken ct = default)
        {
            int maxRows = ReadIntEnv("CSV_MAX_ROWS", 50000);
            int logEvery = ReadIntEnv("CSV_LOG_EVERY", 5000);
            int maxFieldChars = ReadIntEnv("CSV_MAX_FIELD_CHARS", 2000);
            int cmdTimeoutSec = ReadIntEnv("SQL_COMMAND_TIMEOUT_SEC", 600);

            string sqlConnStr = MustGetEnv("SQL_CONNECTION_STRING");

            string csvFileName = $"assurance-report-{timestamp}.csv";
            string csvDir = "./docs/temp_csv";
            if (!Directory.Exists(csvDir))
                Directory.CreateDirectory(csvDir);

            string filePath = Path.Combine(csvDir, csvFileName);

            string sql = "SET NOCOUNT ON; SELECT TOP (@MaxRows) * FROM ( " + AssuranceCoreSql + " ) AS q;";
            Console.WriteLine($"[CSV] Starting query with TOP {maxRows} ...");

            using (var conn = new SqlConnection(sqlConnStr))
            {
                await conn.OpenAsync(ct);
                using var cmd = new SqlCommand(sql, conn)
                {
                    CommandTimeout = cmdTimeoutSec
                };
                cmd.Parameters.AddWithValue("@MaxRows", maxRows);

                using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);
                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1 << 16);
                using var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                int fieldCount = reader.FieldCount;
                for (int i = 0; i < fieldCount; i++)
                {
                    if (i > 0) sw.Write(',');
                    sw.Write(EscapeCsv(reader.GetName(i)));
                }
                await sw.WriteLineAsync();

                long rows = 0;
                while (await reader.ReadAsync(ct))
                {
                    for (int i = 0; i < fieldCount; i++)
                    {
                        if (i > 0) sw.Write(',');

                        if (await reader.IsDBNullAsync(i, ct))
                            continue;

                        object val = reader.GetValue(i);
                        string s = ConvertToString(val);
                        if (s.Length > maxFieldChars)
                            s = s.Substring(0, maxFieldChars) + "…";

                        sw.Write(EscapeCsv(s));
                    }
                    await sw.WriteLineAsync();

                    rows++;
                    if (rows % logEvery == 0)
                    {
                        await sw.FlushAsync();
                        Console.WriteLine($"[CSV] Wrote {rows:N0} rows...");
                    }
                }

                await sw.FlushAsync();
                Console.WriteLine($"[CSV] Done. Total rows written: {rows:N0}");
            }

            return filePath;
        }

        private static string ConvertToString(object value) =>
            value switch
            {
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                bool b => b ? "true" : "false",
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };

        private static string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            bool mustQuote = s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            if (!mustQuote) return s;
            return '"' + s.Replace("\"", "\"\"") + '"';
        }

        private static string MustGetEnv(string name)
        {
            var v = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(v))
                throw new InvalidOperationException($"Missing environment variable: {name}");
            return v;
        }

        private static int ReadIntEnv(string name, int fallback)
        {
            var v = Environment.GetEnvironmentVariable(name);
            return int.TryParse(v, out var n) ? n : fallback;
        }
    }
}
