using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ActionItems.Sdk.ActionItems.Repositories;

public interface IRepository<T> where T : class
{
    IQueryable<T> Query();

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<T?> GetByIdAsync(object[] keyValues, CancellationToken cancellationToken = default);

    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
