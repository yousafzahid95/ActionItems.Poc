using Microsoft.Extensions.Caching.Memory;

namespace ActionItems.Sdk.Sharding.Caching;

/// <summary>
/// In-memory shard lookup cache. Swap <see cref="IShardCache"/> for a Redis implementation in production.
/// </summary>
public sealed class InMemoryShardCache : IShardCache
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(1);
    private readonly IMemoryCache _memoryCache;

    public InMemoryShardCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<ShardInfo?> GetAsync(
        Guid workAreaId,
        ApplicationIntent applicationIntent,
        CancellationToken cancellationToken = default)
    {
        _memoryCache.TryGetValue(GetCacheKey(workAreaId, applicationIntent), out ShardInfo? shardInfo);
        return Task.FromResult(shardInfo);
    }

    public Task SetAsync(Guid workAreaId, ShardInfo shardInfo, CancellationToken cancellationToken = default)
    {
        _memoryCache.Set(GetCacheKey(workAreaId, shardInfo.ApplicationIntent), shardInfo, DefaultExpiration);
        return Task.CompletedTask;
    }

    private static string GetCacheKey(Guid workAreaId, ApplicationIntent applicationIntent) =>
        $"shard:workarea:{workAreaId:D}:intent:{applicationIntent}";
}
