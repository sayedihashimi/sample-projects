using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace FunctionApp1.HealthChecks;

public sealed class HealthCheckDbContext : DbContext
{
    public HealthCheckDbContext(DbContextOptions<HealthCheckDbContext> options) : base(options)
    {
    }

    public DbSet<HealthCheckResult> Results => Set<HealthCheckResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<HealthCheckResult>();

        entity.HasKey(x => x.Id);
        entity.Property(x => x.CheckedUrl).IsRequired();
        entity.Property(x => x.TimestampUtc).IsRequired();
        entity.Property(x => x.IsSuccess).IsRequired();
        entity.Property(x => x.StatusCode);
        entity.Property(x => x.ErrorMessage);

        entity.HasIndex(x => x.TimestampUtc);
    }

    public static string BuildSqliteConnectionString(string baseConnectionString, string absoluteDbPath)
    {
        var builder = new SqliteConnectionStringBuilder(baseConnectionString);

        // Respect explicit DataSource if provided; otherwise, set it.
        if (string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource.Equals("healthchecks.db", StringComparison.OrdinalIgnoreCase))
        {
            builder.DataSource = absoluteDbPath;
        }
        else if (!Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.Combine(AppContext.BaseDirectory, builder.DataSource);
        }

        return builder.ToString();
    }
}
