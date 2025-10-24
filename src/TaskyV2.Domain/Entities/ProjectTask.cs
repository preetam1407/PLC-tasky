using System.ComponentModel.DataAnnotations;

namespace TaskyV2.Domain.Entities;

public class ProjectTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    [Required]
    public string Title { get; set; } = default!;

    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
