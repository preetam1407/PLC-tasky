namespace Tasky.Domain;

public class TaskItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    public TaskItem(string description)
    {
        Description = description.Trim();
        IsCompleted = false;
    }

    public void UpdateDescription(string description) => Description = description.Trim();
    public void Toggle() => IsCompleted = !IsCompleted;
}
