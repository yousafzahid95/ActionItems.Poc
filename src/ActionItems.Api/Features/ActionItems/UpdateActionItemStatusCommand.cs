using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.ActionItems.Services;
using MediatR;

namespace ActionItems.Api.Features.ActionItems;

public sealed record UpdateActionItemStatusCommand(Guid WorkAreaId, Guid ActionItemId, string Status) : IRequest<ActionItem?>;

public sealed class UpdateActionItemStatusHandler : IRequestHandler<UpdateActionItemStatusCommand, ActionItem?>
{
    private readonly IActionItemService _actionItemService;

    public UpdateActionItemStatusHandler(IActionItemService actionItemService)
    {
        _actionItemService = actionItemService;
    }

    public Task<ActionItem?> Handle(UpdateActionItemStatusCommand request, CancellationToken cancellationToken)
    {
        return _actionItemService.UpdateStatusAsync(
            request.WorkAreaId,
            request.ActionItemId,
            request.Status,
            cancellationToken);
    }
}
