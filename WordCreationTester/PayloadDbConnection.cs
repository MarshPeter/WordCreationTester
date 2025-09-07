using Microsoft.EntityFrameworkCore;
using WordCreationTester;

public class PayloadDbConnection : DbContext
{
    public DbSet<AIReportRequestEntity> AIReportRequests { get; set; }
    public DbSet<AIReportResultEntity> AIReportResults { get; set; }
    public DbSet<AIReportStatusEntity> AIReportStatuses { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var connString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

        if (string.IsNullOrEmpty(connString))
            throw new InvalidOperationException("DB_CONNECTION_STRING not set");

        options.UseSqlServer(connString);
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
