namespace Tasky.Application.DTOs;

public record CreateTaskRequest(string Description);
public record UpdateTaskRequest(string Description, bool IsCompleted);
public record TaskResponse(Guid Id, string Description, bool IsCompleted, DateTime CreatedAtUtc);
