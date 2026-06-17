using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.Sharding;
using Microsoft.EntityFrameworkCore;

namespace ActionItems.Sdk.ActionItems.Repositories;

public sealed class EntityRepository : EfRepository<Entity>, IEntityRepository
{
    public EntityRepository(ShardedDbContextHolder holder)
        : base(holder)
    {
    }

    public async Task<Entity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async override Task<IReadOnlyList<Entity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Query()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
