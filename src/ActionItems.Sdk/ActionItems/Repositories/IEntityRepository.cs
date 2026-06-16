using ActionItems.Sdk.ActionItems.Entities;

namespace ActionItems.Sdk.ActionItems.Repositories;

public interface IEntityRepository
{
    Task<Entity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Entity> AddAsync(Entity entity, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
