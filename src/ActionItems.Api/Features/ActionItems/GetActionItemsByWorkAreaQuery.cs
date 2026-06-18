using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.ActionItems.Services;
using MediatR;

namespace ActionItems.Api.Features.ActionItems;

public sealed record GetActionItemsByWorkAreaQuery(Guid WorkAreaId) : IRequest<IReadOnlyList<ActionItem>>;

public sealed class GetActionItemsByWorkAreaHandler : IRequestHandler<GetActionItemsByWorkAreaQuery, IReadOnlyList<ActionItem>>
{
    private readonly IActionItemService _actionItemService;

    public GetActionItemsByWorkAreaHandler(IActionItemService actionItemService)
    {
        _actionItemService = actionItemService;
    }

    public Task<IReadOnlyList<ActionItem>> Handle(GetActionItemsByWorkAreaQuery request, CancellationToken cancellationToken)
    {
        return _actionItemService.GetAllByWorkAreaAsync(request.WorkAreaId, cancellationToken);
    }
}
