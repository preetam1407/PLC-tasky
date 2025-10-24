namespace TaskyV2.Application.DTOs;

public record TaskCreateRequest(string Title, DateTime? DueDate);
public record TaskUpdateRequest(string Title, DateTime? DueDate, bool IsCompleted);
public record TaskResponse(Guid Id, string Title, DateTime? DueDate, bool IsCompleted, DateTime CreatedAtUtc);
