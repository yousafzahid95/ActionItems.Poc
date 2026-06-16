using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.Sharding;
using Microsoft.EntityFrameworkCore;

namespace ActionItems.Sdk.ActionItems.Repositories;

public sealed class EntityRepository : IEntityRepository
{
    private readonly ShardedDbContextHolder _holder;

    public EntityRepository(ShardedDbContextHolder holder)
    {
        _holder = holder;
    }

    public async Task<Entity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _holder.DbContext.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Entity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _holder.DbContext.Entities
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Entity> AddAsync(Entity entity, CancellationToken cancellationToken = default)
    {
        _holder.DbContext.Entities.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _holder.SaveChangesAsync(cancellationToken);
    }
}
