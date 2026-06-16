namespace ActionItems.Sdk.Sharding.Caching;

public interface IShardCache
{
    Task<ShardInfo?> GetAsync(
        Guid workAreaId,
        ApplicationIntent applicationIntent,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        Guid workAreaId,
        ShardInfo shardInfo,
        CancellationToken cancellationToken = default);
}
