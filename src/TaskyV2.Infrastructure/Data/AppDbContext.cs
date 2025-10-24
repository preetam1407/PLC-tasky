using Microsoft.EntityFrameworkCore;
using TaskyV2.Domain.Entities;

namespace TaskyV2.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();

    protected override void OnModelCreating(ModelBuilder b)
    {
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
