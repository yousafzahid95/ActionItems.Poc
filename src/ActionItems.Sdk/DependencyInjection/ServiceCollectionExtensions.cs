using ActionItems.Sdk.ActionItems.DependencyInjection;
using ActionItems.Sdk.Sharding.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ActionItems.Sdk.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddActionItemsSdk(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddActionItemsSharding(configuration);
        services.AddActionItemsPersistence();

        return services;
    }
}
