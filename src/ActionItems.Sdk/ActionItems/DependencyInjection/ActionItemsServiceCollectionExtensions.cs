using ActionItems.Sdk.ActionItems.Repositories;
using ActionItems.Sdk.ActionItems.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ActionItems.Sdk.ActionItems.DependencyInjection;

public static class ActionItemsServiceCollectionExtensions
{
    public static IServiceCollection AddActionItemsPersistence(this IServiceCollection services)
    {
        services.AddScoped<IEntityRepository, EntityRepository>();
        services.AddScoped<IActionItemRepository, ActionItemRepository>();
        services.AddScoped<IActionItemService, ActionItemService>();
        services.AddScoped<IEntityService, EntityService>();

        return services;
    }
}
