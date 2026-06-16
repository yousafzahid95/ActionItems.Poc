using ActionItems.Sdk.ActionItems.Data;
using ActionItems.Sdk.Sharding.Catalog;
using ActionItems.Sdk.Sharding.Catalog.Entities;
using ActionItems.Sdk.Sharding.KeyVault;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ActionItems.Sdk.Sharding;

public static class ShardDatabaseInitializer
{
    /// <summary>
    /// Local POC behavior: always wipe and recreate all databases on host startup.
    /// </summary>
    public static async Task InitializeAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var catalogConnection = configuration.GetConnectionString("ShardCatalog")
            ?? throw new InvalidOperationException("Connection string 'ShardCatalog' is required.");

        var secrets = FileKeyVaultSecretProvider.LoadSecrets(configuration);

        // POC uses separate master/read SQLite files; production uses DBMS replication.
        var shard1Master = ResolveRequiredSecret(secrets, "shard-1-master");
        var shard1Read1 = ResolveRequiredSecret(secrets, "shard-1-read-1");
        var shard1Read2 = ResolveRequiredSecret(secrets, "shard-1-read-2");
        var shard2Master = ResolveRequiredSecret(secrets, "shard-2-master");
        var shard2Read1 = ResolveRequiredSecret(secrets, "shard-2-read-1");
        var shard2Read2 = ResolveRequiredSecret(secrets, "shard-2-read-2");

        var workAreaA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var workAreaB = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await using (var catalogContext = CreateCatalogContext(catalogConnection))
        {
            await catalogContext.Database.EnsureDeletedAsync(cancellationToken);
            await catalogContext.Database.EnsureCreatedAsync(cancellationToken);

            var clientA = "client-1";
            var clientB = "client-2";

            catalogContext.WorkAreaClients.AddRange(
                new WorkAreaClientMapping
                {
                    WorkAreaId = workAreaA,
                    ClientId = clientA
                },
                new WorkAreaClientMapping
                {
                    WorkAreaId = workAreaB,
                    ClientId = clientB
                });

            catalogContext.ClientShards.AddRange(
                new ClientShardMapping
                {
                    ClientId = clientA,
                    ShardKey = "shard-1"
                },
                new ClientShardMapping
                {
                    ClientId = clientB,
                    ShardKey = "shard-2"
                });

            catalogContext.Shards.AddRange(
                new ShardDefinition
                {
                    ShardKey = "shard-1",
                    MasterKeyVaultSecretName = "shard-1-master"
                },
                new ShardDefinition
                {
                    ShardKey = "shard-2",
                    MasterKeyVaultSecretName = "shard-2-master"
                });

            catalogContext.ShardReadReplicas.AddRange(
                new ShardReadReplica
                {
                    ShardKey = "shard-1",
                    KeyVaultSecretName = "shard-1-read-1",
                    Order = 1
                },
                new ShardReadReplica
                {
                    ShardKey = "shard-1",
                    KeyVaultSecretName = "shard-1-read-2",
                    Order = 2
                },
                new ShardReadReplica
                {
                    ShardKey = "shard-2",
                    KeyVaultSecretName = "shard-2-read-1",
                    Order = 1
                },
                new ShardReadReplica
                {
                    ShardKey = "shard-2",
                    KeyVaultSecretName = "shard-2-read-2",
                    Order = 2
                });

            await catalogContext.SaveChangesAsync(cancellationToken);
        }

        await EnsureShardSchemaAlwaysAsync(shard1Master, cancellationToken);
        await EnsureShardSchemaAlwaysAsync(shard1Read1, cancellationToken);
        await EnsureShardSchemaAlwaysAsync(shard1Read2, cancellationToken);
        await EnsureShardSchemaAlwaysAsync(shard2Master, cancellationToken);
        await EnsureShardSchemaAlwaysAsync(shard2Read1, cancellationToken);
        await EnsureShardSchemaAlwaysAsync(shard2Read2, cancellationToken);
    }

    private static async Task EnsureShardSchemaAlwaysAsync(string connectionString, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<ActionItemsDbContext>()
            .UseSqlite(connectionString)
            .Options;

        await using var context = new ActionItemsDbContext(options);
        await context.Database.EnsureDeletedAsync(cancellationToken);
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }

    private static string ResolveRequiredSecret(IReadOnlyDictionary<string, string> secrets, string secretName)
    {
        if (secrets.TryGetValue(secretName, out var value))
        {
            return value;
        }

        throw new InvalidOperationException($"Key Vault secret '{secretName}' is required for database initialization.");
    }

    private static ShardCatalogDbContext CreateCatalogContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ShardCatalogDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new ShardCatalogDbContext(options);
    }
}
