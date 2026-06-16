namespace ActionItems.Sdk.Sharding;

public interface IShardResolver
{
    Task<ShardInfo> ResolveAsync(
        Guid workAreaId,
        ApplicationIntent applicationIntent,
        CancellationToken cancellationToken = default);

    Task<ShardInfo> ResolveForCreationAsync(Guid workAreaId, CancellationToken cancellationToken = default);
}
