using Microsoft.EntityFrameworkCore;
using TaskyV2.Application.DTOs;
using TaskyV2.Domain.Entities;
using TaskyV2.Infrastructure.Data;

namespace TaskyV2.Infrastructure.Services;

public class ProjectService(AppDbContext db)
{
    public async Task<IEnumerable<ProjectResponse>> GetAllAsync(Guid userId, CancellationToken ct = default)
        => await db.Projects.Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new ProjectResponse(p.Id, p.Title, p.Description, p.CreatedAtUtc))
            .ToListAsync(ct);

    public async Task<ProjectResponse> CreateAsync(Guid userId, ProjectCreateRequest req, CancellationToken ct = default)
    {
        var p = new Project { UserId = userId, Title = req.Title, Description = req.Description };
        db.Projects.Add(p);
        await db.SaveChangesAsync(ct);
        return new ProjectResponse(p.Id, p.Title, p.Description, p.CreatedAtUtc);
    }

    public async Task<Project?> GetEntityAsync(Guid userId, Guid projectId, CancellationToken ct = default)
        => await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, ct);

    public async Task<bool> UpdateAsync(Guid userId, Guid projectId, ProjectUpdateRequest req, CancellationToken ct = default)
    {
        var p = await GetEntityAsync(userId, projectId, ct);
        if (p is null) return false;
        p.Title = req.Title; p.Description = req.Description;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        var p = await GetEntityAsync(userId, projectId, ct);
        if (p is null) return false;
        db.Projects.Remove(p);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
