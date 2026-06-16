using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.ActionItems.Repositories;
using ActionItems.Sdk.Sharding;

namespace ActionItems.Sdk.ActionItems.Services;

public sealed class EntityService : IEntityService
{
    private readonly IShardedScope _shardedScope;
    private readonly IEntityRepository _entityRepository;

    public EntityService(IShardedScope shardedScope, IEntityRepository entityRepository)
    {
        _shardedScope = shardedScope;
        _entityRepository = entityRepository;
    }

    public async Task<Entity> CreateAsync(Guid workAreaId, string name, CancellationToken cancellationToken = default)
    {
        await _shardedScope.InitializeAsync(
            workAreaId,
            ApplicationIntent.ReadWrite,
            ShardedRepositoryAccess.Create,
            cancellationToken);

        var entity = new Entity
        {
            Id = Guid.NewGuid(),
            Name = name
        };

        await _entityRepository.AddAsync(entity, cancellationToken);
        await _entityRepository.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Entity?> GetAsync(Guid workAreaId, Guid entityId, CancellationToken cancellationToken = default)
    {
        await _shardedScope.InitializeAsync(workAreaId, ApplicationIntent.Read, cancellationToken: cancellationToken);
        return await _entityRepository.GetByIdAsync(entityId, cancellationToken);
    }

    public async Task<IReadOnlyList<Entity>> GetAllAsync(Guid workAreaId, CancellationToken cancellationToken = default)
    {
        await _shardedScope.InitializeAsync(workAreaId, ApplicationIntent.Read, cancellationToken: cancellationToken);
        return await _entityRepository.GetAllAsync(cancellationToken);
    }
}
