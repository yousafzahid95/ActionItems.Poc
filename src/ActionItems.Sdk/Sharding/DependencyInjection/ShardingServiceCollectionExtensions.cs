using ActionItems.Sdk.Sharding.Caching;
using ActionItems.Sdk.Sharding.Catalog;
using ActionItems.Sdk.Sharding.Clients;
using ActionItems.Sdk.Sharding.KeyVault;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ActionItems.Sdk.Sharding.DependencyInjection;

public static class ShardingServiceCollectionExtensions
{
    public static IServiceCollection AddActionItemsSharding(this IServiceCollection services, IConfiguration configuration)
    {
        var catalogConnection = configuration.GetConnectionString("ShardCatalog")
            ?? throw new InvalidOperationException("Connection string 'ShardCatalog' is required.");

        services.AddMemoryCache();
        services.AddDbContext<ShardCatalogDbContext>(options =>
            options.UseSqlite(catalogConnection));

        services.AddSingleton<IRoundRobinCounter, RoundRobinCounter>();
        services.AddSingleton<IKeyVaultSecretProvider, FileKeyVaultSecretProvider>();
        services.AddSingleton<IWorkAreaClientIdProvider, MockWorkAreaClientIdProvider>();
        services.AddSingleton<IShardCache, InMemoryShardCache>();
        services.AddScoped<IShardResolver, ShardResolver>();
        services.AddScoped<ShardedDbContextHolder>();
        services.AddScoped<IShardedScope, ShardedScope>();

        return services;
    }
}
