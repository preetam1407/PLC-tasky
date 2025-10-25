using Microsoft.EntityFrameworkCore;
using TaskyV2.Application.DTOs;
using TaskyV2.Domain.Entities;
using TaskyV2.Infrastructure.Data;

namespace TaskyV2.Infrastructure.Services;

public class TaskService(AppDbContext db)
{
    private Task<Project?> ProjectOfUser(Guid userId, Guid projectId, CancellationToken ct)
        => db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, ct);

    private Task<ProjectTask?> TaskOfUser(Guid userId, Guid taskId, CancellationToken ct)
        => db.ProjectTasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.Project.UserId == userId, ct);

    private Task<ProjectTask?> TaskOfUser(Guid userId, Guid projectId, Guid taskId, CancellationToken ct)
        => db.ProjectTasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId && t.Project.UserId == userId, ct);

    public async Task<IEnumerable<TaskResponse>> ListAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        if (await ProjectOfUser(userId, projectId, ct) is null) return Enumerable.Empty<TaskResponse>();
        return await db.ProjectTasks.Where(t => t.ProjectId == projectId)
            .AsNoTracking()
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
        return ToResponse(t);
    }

    public async Task<TaskResponse?> GetAsync(Guid userId, Guid taskId, CancellationToken ct = default)
        => (await TaskOfUser(userId, taskId, ct)) is { } t ? ToResponse(t) : null;

    public async Task<bool> UpdateAsync(Guid userId, Guid projectId, Guid taskId, TaskUpdateRequest req, CancellationToken ct = default)
    {
        var t = await TaskOfUser(userId, projectId, taskId, ct);
        if (t is null) return false;
        ApplyUpdate(t, req);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateAsync(Guid userId, Guid taskId, TaskUpdateRequest req, CancellationToken ct = default)
    {
        var t = await TaskOfUser(userId, taskId, ct);
        if (t is null) return false;
        ApplyUpdate(t, req);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ToggleAsync(Guid userId, Guid projectId, Guid taskId, CancellationToken ct = default)
    {
        var t = await TaskOfUser(userId, projectId, taskId, ct);
        if (t is null) return false;
        t.IsCompleted = !t.IsCompleted;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ToggleAsync(Guid userId, Guid taskId, CancellationToken ct = default)
    {
        var t = await TaskOfUser(userId, taskId, ct);
        if (t is null) return false;
        t.IsCompleted = !t.IsCompleted;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid projectId, Guid taskId, CancellationToken ct = default)
    {
        var t = await TaskOfUser(userId, projectId, taskId, ct);
        if (t is null) return false;
        db.ProjectTasks.Remove(t);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid taskId, CancellationToken ct = default)
    {
        var t = await TaskOfUser(userId, taskId, ct);
        if (t is null) return false;
        db.ProjectTasks.Remove(t);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static void ApplyUpdate(ProjectTask task, TaskUpdateRequest req)
    {
        task.Title = req.Title;
        task.DueDate = req.DueDate;
        task.IsCompleted = req.IsCompleted;
    }

    private static TaskResponse ToResponse(ProjectTask task)
        => new(task.Id, task.Title, task.DueDate, task.IsCompleted, task.CreatedAtUtc);
}
