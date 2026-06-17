using ActionItems.Sdk.ActionItems.Data;
using Microsoft.EntityFrameworkCore;

namespace ActionItems.Sdk.Sharding;

public sealed class ShardedScope : IShardedScope
{
    private readonly IShardResolver _shardResolver;
    private readonly ShardedDbContextHolder _holder;

    public ShardedScope(IShardResolver shardResolver, ShardedDbContextHolder holder)
    {
        _shardResolver = shardResolver;
        _holder = holder;
    }

    public bool IsInitialized => _holder.IsInitialized;

    public ApplicationIntent? ApplicationIntent => _holder.ApplicationIntent;

    public async Task InitializeAsync(
        Guid workAreaId,
        ApplicationIntent applicationIntent,
        ShardedRepositoryAccess access = ShardedRepositoryAccess.Read,
        CancellationToken cancellationToken = default)
    {
        if (_holder.IsInitialized && _holder.ApplicationIntent == applicationIntent)
        {
            return;
        }

        if (_holder.IsInitialized)
        {
            await _holder.DisposeAsync();
        }

        var shard = access == ShardedRepositoryAccess.Create
            ? await _shardResolver.ResolveForCreationAsync(workAreaId, cancellationToken)
            : await _shardResolver.ResolveAsync(workAreaId, applicationIntent, cancellationToken);

        var options = new DbContextOptionsBuilder<ActionItemsDbContext>()
            .UseSqlite(shard.ConnectionString)
            .Options;

        var dbContext = new ActionItemsDbContext(options);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        _holder.Attach(dbContext, applicationIntent);
    }

    // Convenience overload that infers repository access from ApplicationIntent
    public Task InitializeAsync(
        Guid workAreaId,
        ApplicationIntent applicationIntent,
        CancellationToken cancellationToken = default)
    {
        // For the convenience overload we never force "Create" behavior.
        // Always resolve via ResolveAsync so ApplicationIntent controls replica vs master selection.
        return InitializeAsync(workAreaId, applicationIntent, ShardedRepositoryAccess.Read, cancellationToken);
    }
}
