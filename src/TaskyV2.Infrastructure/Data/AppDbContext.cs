using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TaskyV2.Domain.Entities;

namespace TaskyV2.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();

    private static readonly GuidToStringConverter GuidConverter = new();
    private static readonly ValueConverter<DateTime, string> DateTimeConverter =
        new ValueConverter<DateTime, string>(
            v => v.ToString("O"),
            v => DateTime.Parse(v, null, System.Globalization.DateTimeStyles.RoundtripKind));

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>()
         .Property(u => u.Id)
         .HasConversion(GuidConverter);

        b.Entity<User>()
         .Property(u => u.CreatedAtUtc)
         .HasConversion(DateTimeConverter);

        b.Entity<Project>()
         .Property(p => p.Id)
         .HasConversion(GuidConverter);

        b.Entity<Project>()
         .Property(p => p.UserId)
         .HasConversion(GuidConverter);

        b.Entity<Project>()
         .Property(p => p.CreatedAtUtc)
         .HasConversion(DateTimeConverter);

        b.Entity<ProjectTask>()
         .Property(t => t.Id)
         .HasConversion(GuidConverter);

        b.Entity<ProjectTask>()
         .Property(t => t.ProjectId)
         .HasConversion(GuidConverter);

        b.Entity<ProjectTask>()
         .Property(t => t.CreatedAtUtc)
         .HasConversion(DateTimeConverter);

        b.Entity<User>()
         .HasIndex(u => u.Email)
         .IsUnique();

        b.Entity<Project>()
         .HasOne(p => p.User)
         .WithMany(u => u.Projects)
         .HasForeignKey(p => p.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ProjectTask>()
         .HasOne(t => t.Project)
         .WithMany(p => p.Tasks)
         .HasForeignKey(t => t.ProjectId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
