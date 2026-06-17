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

    // Convenience overload that infers repository access from ApplicationIntent
    Task InitializeAsync(
        Guid workAreaId,
        ApplicationIntent applicationIntent,
        CancellationToken cancellationToken = default);
}
