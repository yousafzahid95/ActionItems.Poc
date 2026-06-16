namespace ActionItems.Sdk.Sharding;

public interface IShardedScope
{
    bool IsInitialized { get; }

    ApplicationIntent? ApplicationIntent { get; }

    Task InitializeAsync(
        Guid workAreaId,
        ApplicationIntent applicationIntent,
        ShardedRepositoryAccess access = ShardedRepositoryAccess.Read,
        CancellationToken cancellationToken = default);
}
