using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.ActionItems.Services;
using MediatR;

namespace ActionItems.Api.Features.ActionItems;

public sealed record CreateActionItemCommand(Guid WorkAreaId, Guid EntityId, string Title) : IRequest<ActionItem>;

public sealed class CreateActionItemHandler : IRequestHandler<CreateActionItemCommand, ActionItem>
{
    private readonly IActionItemService _actionItemService;

    public CreateActionItemHandler(IActionItemService actionItemService)
    {
        _actionItemService = actionItemService;
    }

    public Task<ActionItem> Handle(CreateActionItemCommand request, CancellationToken cancellationToken)
    {
        return _actionItemService.CreateAsync(request.WorkAreaId, request.EntityId, request.Title, cancellationToken);
    }
}
