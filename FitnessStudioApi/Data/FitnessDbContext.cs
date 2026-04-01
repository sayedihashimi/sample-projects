using FitnessStudioApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FitnessStudioApi.Data;

public class FitnessDbContext(DbContextOptions<FitnessDbContext> options) : DbContext(options)
{
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<ClassType> ClassTypes => Set<ClassType>();
    public DbSet<ClassSchedule> ClassSchedules => Set<ClassSchedule>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MembershipPlan>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.EmergencyContactName).HasMaxLength(200);
        });

        modelBuilder.Entity<Membership>(entity =>
        {
            entity.HasOne(e => e.Member)
                .WithMany(m => m.Memberships)
                .HasForeignKey(e => e.MemberId);
            entity.HasOne(e => e.MembershipPlan)
                .WithMany(p => p.Memberships)
                .HasForeignKey(e => e.MembershipPlanId);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.PaymentStatus).HasConversion<string>();
        });

        modelBuilder.Entity<Instructor>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Bio).HasMaxLength(1000);
        });

        modelBuilder.Entity<ClassType>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DifficultyLevel).HasConversion<string>();
        });

        modelBuilder.Entity<ClassSchedule>(entity =>
        {
            entity.HasOne(e => e.ClassType)
                .WithMany(ct => ct.ClassSchedules)
                .HasForeignKey(e => e.ClassTypeId);
            entity.HasOne(e => e.Instructor)
                .WithMany(i => i.ClassSchedules)
                .HasForeignKey(e => e.InstructorId);
            entity.Property(e => e.Room).HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasOne(e => e.ClassSchedule)
                .WithMany(cs => cs.Bookings)
                .HasForeignKey(e => e.ClassScheduleId);
            entity.HasOne(e => e.Member)
                .WithMany(m => m.Bookings)
                .HasForeignKey(e => e.MemberId);
            entity.Property(e => e.Status).HasConversion<string>();
        });

        // Configure DateTimeOffset properties to use ticks for SQLite compatibility
        var dateTimeOffsetConverter = new DateTimeOffsetToBinaryConverter();
        var nullableDateTimeOffsetConverter = new ValueConverter<DateTimeOffset?, long?>(
            v => v.HasValue ? v.Value.ToUniversalTime().Ticks : null,
            v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType.GetProperties()
                .Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));

            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(DateTimeOffset))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(property.Name)
                        .HasConversion(dateTimeOffsetConverter);
                }
                else
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(property.Name)
                        .HasConversion(nullableDateTimeOffsetConverter);
                }
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt") is { } createdAt)
                    createdAt.CurrentValue = now;
                if (entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt") is { } updatedAt)
                    updatedAt.CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt") is { } updatedAt)
                    updatedAt.CurrentValue = now;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
