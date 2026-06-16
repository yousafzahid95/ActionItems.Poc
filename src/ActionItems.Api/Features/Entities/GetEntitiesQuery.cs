using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.ActionItems.Services;
using MediatR;

namespace ActionItems.Api.Features.Entities;

public sealed record GetEntitiesQuery(Guid WorkAreaId) : IRequest<IReadOnlyList<Entity>>;

public sealed class GetEntitiesHandler : IRequestHandler<GetEntitiesQuery, IReadOnlyList<Entity>>
{
    private readonly IEntityService _entityService;

    public GetEntitiesHandler(IEntityService entityService)
    {
        _entityService = entityService;
    }

    public Task<IReadOnlyList<Entity>> Handle(GetEntitiesQuery request, CancellationToken cancellationToken)
    {
        return _entityService.GetAllAsync(request.WorkAreaId, cancellationToken);
    }
}
