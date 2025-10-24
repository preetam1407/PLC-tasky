using System.ComponentModel.DataAnnotations;

namespace TaskyV2.Domain.Entities;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    [Required, MinLength(3), MaxLength(100)]
    public string Title { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
}
