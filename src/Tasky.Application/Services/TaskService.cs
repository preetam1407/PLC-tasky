using Tasky.Application.Abstractions;
using Tasky.Application.DTOs;
using Tasky.Domain;

namespace Tasky.Application.Services;

public class TaskService
{
    private readonly ITaskRepository _repo;
    public TaskService(ITaskRepository repo) => _repo = repo;

    public async Task<IEnumerable<TaskResponse>> GetAllAsync(CancellationToken ct = default) =>
        (await _repo.GetAllAsync(ct)).Select(ToResponse);

    public async Task<TaskResponse?> GetAsync(Guid id, CancellationToken ct = default)
        => (await _repo.GetByIdAsync(id, ct)) is { } t ? ToResponse(t) : null;

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest req, CancellationToken ct = default)
    {
        var item = new TaskItem(req.Description);
        await _repo.AddAsync(item, ct);
        return ToResponse(item);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateTaskRequest req, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null) return false;
        existing.UpdateDescription(req.Description);
        if (existing.IsCompleted != req.IsCompleted) existing.Toggle();
        await _repo.UpdateAsync(existing, ct);
        return true;
    }

    public async Task<bool> ToggleAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null) return false;
        existing.Toggle();
        await _repo.UpdateAsync(existing, ct);
        return true;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        => _repo.DeleteAsync(id, ct);

    private static TaskResponse ToResponse(TaskItem t)
        => new(t.Id, t.Description, t.IsCompleted, t.CreatedAtUtc);
}
