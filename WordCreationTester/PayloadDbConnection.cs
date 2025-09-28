using Microsoft.EntityFrameworkCore;
using WordCreationTester;

public class PayloadDbConnection : DbContext
{
    private readonly string connectionString;

    public PayloadDbConnection(AIConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        connectionString = config.DbConnectionString ?? throw new InvalidOperationException("DB_CONNECTION_STRING not set");
    }

    public DbSet<AIReportRequestEntity> AIReportRequests { get; set; }
    public DbSet<AIReportResultEntity> AIReportResults { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AIReportRequestEntity>()
            .HasKey(r => r.AIRequestId);

        modelBuilder.Entity<AIReportResultEntity>()
            .HasKey(r => r.ResultId);

        modelBuilder.Entity<AIReportResultEntity>()
            .HasOne<AIReportRequestEntity>()
            .WithMany()
            .HasForeignKey(r => r.AIRequestId);
    }
}
