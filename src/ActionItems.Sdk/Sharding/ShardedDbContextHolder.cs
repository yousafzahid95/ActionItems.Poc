using ActionItems.Sdk.ActionItems.Data;

namespace ActionItems.Sdk.Sharding;

public sealed class ShardedDbContextHolder : IAsyncDisposable
{
    private ActionItemsDbContext? _dbContext;

    public ApplicationIntent? ApplicationIntent { get; private set; }

    public ActionItemsDbContext DbContext =>
        _dbContext ?? throw new InvalidOperationException(
            "The sharded database context has not been initialized. Call IShardedScope.InitializeAsync first.");

    public bool IsInitialized => _dbContext is not null;

    internal void Attach(ActionItemsDbContext dbContext, ApplicationIntent applicationIntent)
    {
        _dbContext = dbContext;
        ApplicationIntent = applicationIntent;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return DbContext.SaveChangesAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        if (_dbContext is null)
        {
            ApplicationIntent = null;
            return ValueTask.CompletedTask;
        }

        var context = _dbContext;
        _dbContext = null;
        ApplicationIntent = null;
        return context.DisposeAsync();
    }
}
