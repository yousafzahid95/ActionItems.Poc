using ActionItems.Api.Features.ActionItems;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ActionItems.Api.Controllers;

[ApiController]
[Route("api/workareas/{workAreaId:guid}/action-items")]
[Produces("application/json")]
public sealed class ActionItemsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ActionItemsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        Guid workAreaId,
        [FromBody] CreateActionItemRequest request,
        CancellationToken cancellationToken)
    {
        var actionItem = await _mediator.Send(
            new CreateActionItemCommand(workAreaId, request.EntityId, request.Title),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { workAreaId, actionItemId = actionItem.Id }, actionItem);
    }

    [HttpGet("{actionItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid workAreaId, Guid actionItemId, CancellationToken cancellationToken)
    {
        var actionItem = await _mediator.Send(new GetActionItemQuery(workAreaId, actionItemId), cancellationToken);
        return actionItem is null ? NotFound() : Ok(actionItem);
    }

    [HttpGet("by-entity/{entityId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEntity(Guid workAreaId, Guid entityId, CancellationToken cancellationToken)
    {
        var actionItems = await _mediator.Send(new GetActionItemsByEntityQuery(workAreaId, entityId), cancellationToken);
        return Ok(actionItems);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllByWorkArea(Guid workAreaId, CancellationToken cancellationToken)
    {
        var actionItems = await _mediator.Send(new ActionItems.Api.Features.ActionItems.GetActionItemsByWorkAreaQuery(workAreaId), cancellationToken);
        return Ok(actionItems);
    }

    [HttpPatch("{actionItemId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid workAreaId,
        Guid actionItemId,
        [FromBody] UpdateActionItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var actionItem = await _mediator.Send(
            new UpdateActionItemStatusCommand(workAreaId, actionItemId, request.Status),
            cancellationToken);

        return actionItem is null ? NotFound() : Ok(actionItem);
    }
}

public sealed record CreateActionItemRequest(Guid EntityId, string Title);

public sealed record UpdateActionItemStatusRequest(string Status);
