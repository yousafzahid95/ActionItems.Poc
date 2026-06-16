using ActionItems.Sdk.ActionItems.Entities;

namespace ActionItems.Sdk.ActionItems.Services;

public interface IEntityService
{
    Task<Entity> CreateAsync(Guid workAreaId, string name, CancellationToken cancellationToken = default);
    Task<Entity?> GetAsync(Guid workAreaId, Guid entityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entity>> GetAllAsync(Guid workAreaId, CancellationToken cancellationToken = default);
}
