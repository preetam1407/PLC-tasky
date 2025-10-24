using Microsoft.EntityFrameworkCore;
using TaskyV2.Infrastructure.Data;

namespace TaskyV2.Infrastructure.Services;

// ---------- Request/Response contracts ----------
public record ScheduleInput(
    DateTime? StartDate,
    DateTime? EndDate,
    int DailyCapacity = 5,
    string[]? WorkingDays = null
);

public record DayPlan(DateTime Date, List<Guid> TaskIds);

public record ScheduleResponse(
    Guid ProjectId,
    DateTime GeneratedAtUtc,
    List<DayPlan> Days
);

// ---------- Scheduler service ----------
public class SchedulerService(AppDbContext db)
{
    private static readonly string[] DefaultWorkingDays = ["Mon","Tue","Wed","Thu","Fri"];

    public async Task<ScheduleResponse?> GenerateAsync(
        Guid userId,
        Guid projectId,
        ScheduleInput input,
        CancellationToken ct = default)
    {
        // Ownership + eager-load tasks
        var project = await db.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, ct);

        if (project is null) return null;

        // Get only incomplete tasks, ordered by DueDate (null last) then CreatedAt
        var pending = project.Tasks
            .Where(t => !t.IsCompleted)
            .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenBy(t => t.CreatedAtUtc)
            .Select(t => t.Id)
            .ToList();

        // No tasks to plan? Return empty plan.
        if (pending.Count == 0)
            return new ScheduleResponse(projectId, DateTime.UtcNow, new());

        // Normalize inputs
        var cap = Math.Max(1, input.DailyCapacity); // at least 1 per day
        var wd = NormalizeWorkingDays(input.WorkingDays);

        var start = (input.StartDate?.Date) ?? DateTime.UtcNow.Date;

        // If at least one due date exists, end = max(latestDue, start); else default to start+7
        var latestDue = project.Tasks
            .Where(t => t.DueDate.HasValue && !t.IsCompleted)
            .Select(t => t.DueDate!.Value.Date)
            .DefaultIfEmpty(start.AddDays(7))
            .Max();

        var end = (input.EndDate?.Date) ?? latestDue;
        if (end < start) end = start;

        // Pack tasks into working-day buckets
        var days = new List<DayPlan>();
        var queue = new Queue<Guid>(pending);
        var cursor = start;

        while (cursor <= end && queue.Count > 0)
        {
            if (IsWorkingDay(cursor, wd))
            {
                var bucket = new DayPlan(cursor, new());
                for (int i = 0; i < cap && queue.Count > 0; i++)
                {
                    bucket.TaskIds.Add(queue.Dequeue());
                }
                days.Add(bucket);
            }
            cursor = cursor.AddDays(1);
        }

        // If tasks remain after 'end', attach them to the last working day bucket
        if (queue.Count > 0 && days.Count > 0)
        {
            days[^1].TaskIds.AddRange(queue);
        }

        return new ScheduleResponse(projectId, DateTime.UtcNow, days);
    }

    // --- helpers ---

    private static HashSet<string> NormalizeWorkingDays(string[]? days)
    {
        // Accept anything like "Mon", "Monday", case-insensitive; fallback Mon-Fri
        var input = (days is { Length: > 0 } ? days : DefaultWorkingDays);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var d in input)
        {
            if (string.IsNullOrWhiteSpace(d)) continue;
            var key = d.Trim();
            // use 3-letter prefix to compare with ToString("ddd")
            set.Add(key.Length >= 3 ? key[..3] : key);
        }
        return set;
    }

    private static bool IsWorkingDay(DateTime date, HashSet<string> wd)
    {
        // "Mon","Tue","Wed","Thu","Fri","Sat","Sun" (current culture 3-letter)
        var ddd = date.ToString("ddd"); // e.g., "Mon"
        return wd.Contains(ddd);
    }
}
