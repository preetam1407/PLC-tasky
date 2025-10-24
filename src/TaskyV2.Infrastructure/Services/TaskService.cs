using Microsoft.EntityFrameworkCore;
using TaskyV2.Application.DTOs;
using TaskyV2.Domain.Entities;
using TaskyV2.Infrastructure.Data;

namespace TaskyV2.Infrastructure.Services;

public class TaskService(AppDbContext db)
{
    private async Task<Project?> ProjectOfUser(Guid userId, Guid projectId, CancellationToken ct)
        => await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, ct);

    public async Task<IEnumerable<TaskResponse>> ListAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        if (await ProjectOfUser(userId, projectId, ct) is null) return Enumerable.Empty<TaskResponse>();
        return await db.ProjectTasks.Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenBy(t => t.CreatedAtUtc)
            .Select(t => new TaskResponse(t.Id, t.Title, t.DueDate, t.IsCompleted, t.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<TaskResponse?> CreateAsync(Guid userId, Guid projectId, TaskCreateRequest req, CancellationToken ct = default)
    {
        if (await ProjectOfUser(userId, projectId, ct) is null) return null;
        var t = new ProjectTask { ProjectId = projectId, Title = req.Title, DueDate = req.DueDate };
        db.ProjectTasks.Add(t);
        await db.SaveChangesAsync(ct);
        return new TaskResponse(t.Id, t.Title, t.DueDate, t.IsCompleted, t.CreatedAtUtc);
    }

    public async Task<bool> UpdateAsync(Guid userId, Guid projectId, Guid taskId, TaskUpdateRequest req, CancellationToken ct = default)
    {
        if (await ProjectOfUser(userId, projectId, ct) is null) return false;
        var t = await db.ProjectTasks.FirstOrDefaultAsync(x => x.Id == taskId && x.ProjectId == projectId, ct);
        if (t is null) return false;
        t.Title = req.Title; t.DueDate = req.DueDate; t.IsCompleted = req.IsCompleted;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ToggleAsync(Guid userId, Guid projectId, Guid taskId, CancellationToken ct = default)
    {
        if (await ProjectOfUser(userId, projectId, ct) is null) return false;
        var t = await db.ProjectTasks.FirstOrDefaultAsync(x => x.Id == taskId && x.ProjectId == projectId, ct);
        if (t is null) return false;
        t.IsCompleted = !t.IsCompleted;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid projectId, Guid taskId, CancellationToken ct = default)
    {
        if (await ProjectOfUser(userId, projectId, ct) is null) return false;
        var t = await db.ProjectTasks.FirstOrDefaultAsync(x => x.Id == taskId && x.ProjectId == projectId, ct);
        if (t is null) return false;
        db.ProjectTasks.Remove(t);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
