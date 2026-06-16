using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.ActionItems.Services;
using MediatR;

namespace ActionItems.Api.Features.ActionItems;

public sealed record GetActionItemQuery(Guid WorkAreaId, Guid ActionItemId) : IRequest<ActionItem?>;

public sealed class GetActionItemHandler : IRequestHandler<GetActionItemQuery, ActionItem?>
{
    private readonly IActionItemService _actionItemService;

    public GetActionItemHandler(IActionItemService actionItemService)
    {
        _actionItemService = actionItemService;
    }

    public Task<ActionItem?> Handle(GetActionItemQuery request, CancellationToken cancellationToken)
    {
        return _actionItemService.GetAsync(request.WorkAreaId, request.ActionItemId, cancellationToken);
    }
}
