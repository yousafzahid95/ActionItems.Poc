using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.ActionItems.Repositories;
using ActionItems.Sdk.Sharding;

namespace ActionItems.Sdk.ActionItems.Services;

public sealed class ActionItemService : IActionItemService
{
    private readonly IShardedScope _shardedScope;
    private readonly IActionItemRepository _actionItemRepository;

    public ActionItemService(IShardedScope shardedScope, IActionItemRepository actionItemRepository)
    {
        _shardedScope = shardedScope;
        _actionItemRepository = actionItemRepository;
    }

    public async Task<ActionItem> CreateAsync(
        Guid workAreaId,
        Guid entityId,
        string title,
        CancellationToken cancellationToken = default)
    {
        await _shardedScope.InitializeAsync(
            workAreaId,
            ApplicationIntent.ReadWrite,
            ShardedRepositoryAccess.Create,
            cancellationToken);

        var actionItem = new ActionItem
        {
            Id = Guid.NewGuid(),
            WorkAreaId = workAreaId,
            EntityId = entityId,
            Title = title,
            Status = "Open",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _actionItemRepository.AddAsync(actionItem, cancellationToken);
        await _actionItemRepository.SaveChangesAsync(cancellationToken);
        return actionItem;
    }

    public async Task<ActionItem?> GetAsync(Guid workAreaId, Guid actionItemId, CancellationToken cancellationToken = default)
    {
        await _shardedScope.InitializeAsync(workAreaId, ApplicationIntent.Read, cancellationToken: cancellationToken);
        return await _actionItemRepository.GetByIdAsync(actionItemId, cancellationToken);
    }

    public async Task<IReadOnlyList<ActionItem>> GetByEntityAsync(
        Guid workAreaId,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        await _shardedScope.InitializeAsync(workAreaId, ApplicationIntent.Read, cancellationToken: cancellationToken);
        return await _actionItemRepository.GetByEntityIdAsync(entityId, cancellationToken);
    }

    public async Task<ActionItem?> UpdateStatusAsync(
        Guid workAreaId,
        Guid actionItemId,
        string status,
        CancellationToken cancellationToken = default)
    {
        await _shardedScope.InitializeAsync(
            workAreaId,
            ApplicationIntent.ReadWrite,
            ShardedRepositoryAccess.Create,
            cancellationToken);

        var actionItem = await _actionItemRepository.UpdateStatusAsync(actionItemId, status, cancellationToken);
        if (actionItem is null)
        {
            return null;
        }

        await _actionItemRepository.SaveChangesAsync(cancellationToken);
        return actionItem;
    }
}
