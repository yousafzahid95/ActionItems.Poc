using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.Sharding;
using Microsoft.EntityFrameworkCore;

namespace ActionItems.Sdk.ActionItems.Repositories;

public sealed class ActionItemRepository : IActionItemRepository
{
    private readonly ShardedDbContextHolder _holder;

    public ActionItemRepository(ShardedDbContextHolder holder)
    {
        _holder = holder;
    }

    public async Task<ActionItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _holder.DbContext.ActionItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ActionItem>> GetByEntityIdAsync(Guid entityId, CancellationToken cancellationToken = default)
    {
        return await _holder.DbContext.ActionItems
            .AsNoTracking()
            .Where(x => x.EntityId == entityId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<ActionItem> AddAsync(ActionItem actionItem, CancellationToken cancellationToken = default)
    {
        _holder.DbContext.ActionItems.Add(actionItem);
        return Task.FromResult(actionItem);
    }

    public async Task<ActionItem?> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
    {
        var actionItem = await _holder.DbContext.ActionItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (actionItem is null)
        {
            return null;
        }

        actionItem.Status = status;
        actionItem.UpdatedAtUtc = DateTime.UtcNow;
        return actionItem;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _holder.SaveChangesAsync(cancellationToken);
    }
}
