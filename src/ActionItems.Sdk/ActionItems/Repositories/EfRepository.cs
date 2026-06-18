using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using ActionItems.Sdk.Sharding;
using Microsoft.EntityFrameworkCore;

namespace ActionItems.Sdk.ActionItems.Repositories;

public class EfRepository<T> : IRepository<T> where T : class
{
    protected readonly ShardedDbContextHolder _holder;

    public EfRepository(ShardedDbContextHolder holder)
    {
        _holder = holder;
    }

    public IQueryable<T> Query()
    {
        return _holder.DbContext.Set<T>().AsNoTracking();
    }

    public async virtual Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _holder.DbContext.Set<T>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<T?> GetByIdAsync(object[] keyValues, CancellationToken cancellationToken = default)
    {
        // Use FindAsync on the tracked set (no AsNoTracking) to get by key
        return await _holder.DbContext.Set<T>().FindAsync(keyValues, cancellationToken);
    }

    public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        _holder.DbContext.Set<T>().Add(entity);
        return Task.FromResult(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _holder.SaveChangesAsync(cancellationToken);
    }
}
