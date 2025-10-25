namespace TaskyV2.Application.DTOs;

public record ProjectCreateRequest(string Title, string? Description);
public record ProjectUpdateRequest(string Title, string? Description);
public record ProjectResponse(Guid Id, string Title, string? Description, DateTime CreatedAtUtc);
public record ProjectDetailResponse(Guid Id, string Title, string? Description, DateTime CreatedAtUtc, IReadOnlyList<TaskResponse> Tasks);
