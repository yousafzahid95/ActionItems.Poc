using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.Sharding;
using Microsoft.EntityFrameworkCore;

namespace ActionItems.Sdk.ActionItems.Repositories;

public class ActionItemRepository : EfRepository<ActionItem>, IActionItemRepository
{
    public ActionItemRepository(ShardedDbContextHolder holder)
        : base(holder)
    {
    }

    public async Task<ActionItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ActionItem>> GetByEntityIdAsync(Guid entityId, CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(x => x.EntityId == entityId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActionItem>> GetAllByWorkAreaAsync(Guid workAreaId, CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(x => x.WorkAreaId == workAreaId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
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
}
