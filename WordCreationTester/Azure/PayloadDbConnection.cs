using Microsoft.EntityFrameworkCore;
using WordCreationTester.Configuration;
using WordCreationTester.DTO;

public class PayloadDbConnection : DbContext
{
    private readonly string connectionString;

    public PayloadDbConnection(AIConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        connectionString = config.DbConnectionString ?? throw new InvalidOperationException("DB_CONNECTION_STRING not set");
    }

    public DbSet<AIReportRequest> AIReportRequest { get; set; }
    public DbSet<AIReportIndexes> AIReportIndexes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AIReportRequest>()
            .HasKey(r => r.Id);

        modelBuilder.Entity<AIReportIndexes>()
            .HasKey(r => r.Id);
    }
}
