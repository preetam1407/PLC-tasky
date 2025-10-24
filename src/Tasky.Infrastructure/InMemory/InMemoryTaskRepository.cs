using System.Collections.Concurrent;
using Tasky.Application.Abstractions;
using Tasky.Domain;

namespace Tasky.Infrastructure.InMemory;

public class InMemoryTaskRepository : ITaskRepository
{
    private readonly ConcurrentDictionary<Guid, TaskItem> _store = new();

    public Task<IEnumerable<TaskItem>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(_store.Values.AsEnumerable());

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var item) ? item : null);

    public Task AddAsync(TaskItem item, CancellationToken ct = default)
    {
        _store[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TaskItem item, CancellationToken ct = default)
    {
        _store[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryRemove(id, out _));
}
