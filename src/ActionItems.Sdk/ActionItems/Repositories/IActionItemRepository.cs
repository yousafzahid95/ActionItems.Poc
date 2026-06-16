using ActionItems.Sdk.ActionItems.Entities;

namespace ActionItems.Sdk.ActionItems.Repositories;

public interface IActionItemRepository
{
    Task<ActionItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActionItem>> GetByEntityIdAsync(Guid entityId, CancellationToken cancellationToken = default);
    Task<ActionItem> AddAsync(ActionItem actionItem, CancellationToken cancellationToken = default);
    Task<ActionItem?> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
