using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.ActionItems.Services;
using MediatR;

namespace ActionItems.Api.Features.ActionItems;

public sealed record GetActionItemsByEntityQuery(Guid WorkAreaId, Guid EntityId) : IRequest<IReadOnlyList<ActionItem>>;

public sealed class GetActionItemsByEntityHandler : IRequestHandler<GetActionItemsByEntityQuery, IReadOnlyList<ActionItem>>
{
    private readonly IActionItemService _actionItemService;

    public GetActionItemsByEntityHandler(IActionItemService actionItemService)
    {
        _actionItemService = actionItemService;
    }

    public Task<IReadOnlyList<ActionItem>> Handle(GetActionItemsByEntityQuery request, CancellationToken cancellationToken)
    {
        return _actionItemService.GetByEntityAsync(request.WorkAreaId, request.EntityId, cancellationToken);
    }
}
