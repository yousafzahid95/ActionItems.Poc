using ActionItems.Sdk.ActionItems.Entities;

namespace ActionItems.Sdk.ActionItems.Services;

public interface IActionItemService
{
    Task<ActionItem> CreateAsync(Guid workAreaId, Guid entityId, string title, CancellationToken cancellationToken = default);
    Task<ActionItem?> GetAsync(Guid workAreaId, Guid actionItemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActionItem>> GetByEntityAsync(Guid workAreaId, Guid entityId, CancellationToken cancellationToken = default);
    Task<ActionItem?> UpdateStatusAsync(Guid workAreaId, Guid actionItemId, string status, CancellationToken cancellationToken = default);
}
