using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.ActionItems.Services;
using MediatR;

namespace ActionItems.Api.Features.Entities;

public sealed record GetEntityQuery(Guid WorkAreaId, Guid EntityId) : IRequest<Entity?>;

public sealed class GetEntityHandler : IRequestHandler<GetEntityQuery, Entity?>
{
    private readonly IEntityService _entityService;

    public GetEntityHandler(IEntityService entityService)
    {
        _entityService = entityService;
    }

    public Task<Entity?> Handle(GetEntityQuery request, CancellationToken cancellationToken)
    {
        return _entityService.GetAsync(request.WorkAreaId, request.EntityId, cancellationToken);
    }
}
