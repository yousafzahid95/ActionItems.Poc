using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.ActionItems.Services;
using MediatR;

namespace ActionItems.Api.Features.Entities;

public sealed record CreateEntityCommand(Guid WorkAreaId, string Name) : IRequest<Entity>;

public sealed class CreateEntityHandler : IRequestHandler<CreateEntityCommand, Entity>
{
    private readonly IEntityService _entityService;

    public CreateEntityHandler(IEntityService entityService)
    {
        _entityService = entityService;
    }

    public Task<Entity> Handle(CreateEntityCommand request, CancellationToken cancellationToken)
    {
        return _entityService.CreateAsync(request.WorkAreaId, request.Name, cancellationToken);
    }
}
