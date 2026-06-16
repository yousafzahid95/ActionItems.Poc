using ActionItems.Api.Features.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ActionItems.Api.Controllers;

[ApiController]
[Route("api/workareas/{workAreaId:guid}/entities")]
[Produces("application/json")]
public sealed class EntitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EntitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        Guid workAreaId,
        [FromBody] CreateEntityRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _mediator.Send(new CreateEntityCommand(workAreaId, request.Name), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { workAreaId, entityId = entity.Id }, entity);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid workAreaId, CancellationToken cancellationToken)
    {
        var entities = await _mediator.Send(new GetEntitiesQuery(workAreaId), cancellationToken);
        return Ok(entities);
    }

    [HttpGet("{entityId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid workAreaId, Guid entityId, CancellationToken cancellationToken)
    {
        var entity = await _mediator.Send(new GetEntityQuery(workAreaId, entityId), cancellationToken);
        return entity is null ? NotFound() : Ok(entity);
    }
}

public sealed record CreateEntityRequest(string Name);
