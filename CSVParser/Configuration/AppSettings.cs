namespace CsvParser.Configuration
{
    public class AppSettings
    {
        public int CsvMaxRows { get; set; } = 50000;
        public int CsvLogEvery { get; set; } = 5000;
        public int CsvMaxFieldChars { get; set; } = 2000;
        public int SqlCommandTimeoutSec { get; set; } = 600;
        public string OutputDirectory { get; set; } = "./docs/temp_csv";
        public string BlobContainerBaseName { get; set; } = "reports";
    }
}