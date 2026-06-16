using ActionItems.Sdk.Sharding.Caching;
using ActionItems.Sdk.Sharding.Catalog;
using ActionItems.Sdk.Sharding.Catalog.Entities;
using ActionItems.Sdk.Sharding.Clients;
using ActionItems.Sdk.Sharding.KeyVault;
using Microsoft.EntityFrameworkCore;

namespace ActionItems.Sdk.Sharding;

public sealed class ShardResolver : IShardResolver
{
    private readonly ShardCatalogDbContext _catalog;
    private readonly IRoundRobinCounter _roundRobinCounter;
    private readonly IKeyVaultSecretProvider _keyVaultSecretProvider;
    private readonly IShardCache _shardCache;
    private readonly IWorkAreaClientIdProvider _workAreaClientIdProvider;

    public ShardResolver(
        ShardCatalogDbContext catalog,
        IRoundRobinCounter roundRobinCounter,
        IKeyVaultSecretProvider keyVaultSecretProvider,
        IShardCache shardCache,
        IWorkAreaClientIdProvider workAreaClientIdProvider)
    {
        _catalog = catalog;
        _roundRobinCounter = roundRobinCounter;
        _keyVaultSecretProvider = keyVaultSecretProvider;
        _shardCache = shardCache;
        _workAreaClientIdProvider = workAreaClientIdProvider;
    }

    public async Task<ShardInfo> ResolveAsync(
        Guid workAreaId,
        ApplicationIntent applicationIntent,
        CancellationToken cancellationToken = default)
    {
        var cached = await _shardCache.GetAsync(workAreaId, applicationIntent, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var workAreaClient = await _catalog.WorkAreaClients
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.WorkAreaId == workAreaId, cancellationToken);

        if (workAreaClient is null)
        {
            var clientId = await _workAreaClientIdProvider.GetClientIdAsync(workAreaId, cancellationToken);
            if (clientId is null)
            {
                throw new InvalidOperationException($"No client mapping found for WorkAreaId '{workAreaId}'.");
            }

            _catalog.WorkAreaClients.Add(new WorkAreaClientMapping
            {
                WorkAreaId = workAreaId,
                ClientId = clientId
            });
            await _catalog.SaveChangesAsync(cancellationToken);

            workAreaClient = new WorkAreaClientMapping { WorkAreaId = workAreaId, ClientId = clientId };
        }

        var clientShard = await _catalog.ClientShards
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == workAreaClient.ClientId, cancellationToken);

        if (clientShard is null)
        {
            throw new InvalidOperationException($"Client '{workAreaClient.ClientId}' is not mapped to any shard.");
        }

        var shardInfo = await BuildShardInfoAsync(
            workAreaId,
            clientShard.ShardKey,
            applicationIntent,
            cancellationToken);

        await _shardCache.SetAsync(workAreaId, shardInfo, cancellationToken);
        return shardInfo;
    }

    public Task<ShardInfo> ResolveForCreationAsync(Guid workAreaId, CancellationToken cancellationToken = default)
    {
        return ResolveForCreationInternalAsync(workAreaId, cancellationToken);
    }

    private async Task<ShardInfo> ResolveForCreationInternalAsync(
        Guid workAreaId,
        CancellationToken cancellationToken)
    {
        var cached = await _shardCache.GetAsync(workAreaId, ApplicationIntent.ReadWrite, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var workAreaClient = await _catalog.WorkAreaClients
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.WorkAreaId == workAreaId, cancellationToken);

        string clientId;
        if (workAreaClient is not null)
        {
            clientId = workAreaClient.ClientId;
        }
        else
        {
            clientId = await _workAreaClientIdProvider.GetClientIdAsync(workAreaId, cancellationToken)
                        ?? GenerateClientId(workAreaId);

            _catalog.WorkAreaClients.Add(new WorkAreaClientMapping
            {
                WorkAreaId = workAreaId,
                ClientId = clientId
            });

            await _catalog.SaveChangesAsync(cancellationToken);
        }

        var clientShard = await _catalog.ClientShards
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == clientId, cancellationToken);

        var shardKey = clientShard?.ShardKey;
        if (shardKey is null)
        {
            var shards = await _catalog.Shards
                .AsNoTracking()
                .OrderBy(x => x.ShardKey)
                .ToListAsync(cancellationToken);

            if (shards.Count == 0)
            {
                throw new InvalidOperationException("No shards are registered in the catalog.");
            }

            var selected = shards[(_roundRobinCounter.Next() - 1) % shards.Count];
            shardKey = selected.ShardKey;

            _catalog.ClientShards.Add(new ClientShardMapping
            {
                ClientId = clientId,
                ShardKey = shardKey
            });
            await _catalog.SaveChangesAsync(cancellationToken);
        }

        var shardInfo = await BuildShardInfoAsync(
            workAreaId,
            shardKey,
            ApplicationIntent.ReadWrite,
            cancellationToken);

        await _shardCache.SetAsync(workAreaId, shardInfo, cancellationToken);
        return shardInfo;
    }

    private static string GenerateClientId(Guid workAreaId)
    {
        // Stable, deterministic "client id" for the POC when the external service doesn't return one.
        var n = workAreaId.ToString("N");
        return $"client-{n[..8]}";
    }

    private async Task<ShardInfo> BuildShardInfoAsync(
        Guid workAreaId,
        string shardKey,
        ApplicationIntent applicationIntent,
        CancellationToken cancellationToken)
    {
        var shardDefinition = await _catalog.Shards
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShardKey == shardKey, cancellationToken);

        if (shardDefinition is null)
        {
            throw new InvalidOperationException($"No shard definition found for ShardKey '{shardKey}'.");
        }

        var secretName = applicationIntent == ApplicationIntent.Read
            ? await ResolveReadSecretNameAsync(shardDefinition, cancellationToken)
            : shardDefinition.MasterKeyVaultSecretName;

        var connectionString = await _keyVaultSecretProvider.GetSecretAsync(secretName, cancellationToken);

        return new ShardInfo(workAreaId, shardKey, connectionString, applicationIntent);
    }

    /// <summary>
    /// Resolves a read connection. Uses round-robin across configured replicas when present;
    /// falls back to master when a shard has no read replicas (replicas are optional).
    /// </summary>
    private async Task<string> ResolveReadSecretNameAsync(
        ShardDefinition shardDefinition,
        CancellationToken cancellationToken)
    {
        var replicas = await _catalog.ShardReadReplicas
            .AsNoTracking()
            .Where(x => x.ShardKey == shardDefinition.ShardKey)
            .OrderBy(x => x.Order)
            .ToListAsync(cancellationToken);

        if (replicas.Count == 0)
        {
            return shardDefinition.MasterKeyVaultSecretName;
        }

        var index = (_roundRobinCounter.Next() - 1) % replicas.Count;
        return replicas[index].KeyVaultSecretName;
    }
}
