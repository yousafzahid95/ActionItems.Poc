using ActionItems.Sdk.ActionItems.Entities;
using ActionItems.Sdk.ActionItems.Services;
using ActionItems.Sdk.Sharding;
using ActionItems.Worker.Events;
using MediatR;

namespace ActionItems.Worker.Features.Events;

public sealed record ProcessActionItemStatusChangedCommand(ActionItemStatusChangedEvent Event) : IRequest<ActionItem?>;

public sealed class ProcessActionItemStatusChangedHandler
    : IRequestHandler<ProcessActionItemStatusChangedCommand, ActionItem?>
{
    private readonly IShardedScope _shardedScope;
    private readonly IActionItemService _actionItemService;
    private readonly ILogger<ProcessActionItemStatusChangedHandler> _logger;

    public ProcessActionItemStatusChangedHandler(
        IShardedScope shardedScope,
        IActionItemService actionItemService,
        ILogger<ProcessActionItemStatusChangedHandler> logger)
    {
        _shardedScope = shardedScope;
        _actionItemService = actionItemService;
        _logger = logger;
    }

    public async Task<ActionItem?> Handle(ProcessActionItemStatusChangedCommand request, CancellationToken cancellationToken)
    {
        var @event = request.Event;

        _logger.LogInformation(
            "Processing status change for ActionItem {ActionItemId} in WorkArea {WorkAreaId}",
            @event.ActionItemId,
            @event.WorkAreaId);

        // Worker mutations always target the master database.
        await _shardedScope.InitializeAsync(
            @event.WorkAreaId,
            ApplicationIntent.ReadWrite,
            ShardedRepositoryAccess.Create,
            cancellationToken: cancellationToken);

        return await _actionItemService.UpdateStatusAsync(
            @event.WorkAreaId,
            @event.ActionItemId,
            @event.NewStatus,
            cancellationToken);
    }
}
