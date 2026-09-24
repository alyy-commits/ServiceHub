using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub.API.Contracts;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Features.Services.Commands;
using ServiceHub.Application.Features.Services.Queries;
using ServiceHub.Domain.Constants;

namespace ServiceHub.API.Controllers;

[ApiController]
[Route("api/services")]
[Authorize]
[Produces("application/json")]
public class ServicesController : ControllerBase
{
    private readonly ISender _sender;

    public ServicesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lists bookable services. Admins can pass includeInactive=true to see inactive ones too.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetAll([FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetAvailableServicesQuery(includeInactive), cancellationToken));
    }

    /// <summary>Gets a service by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetServiceByIdQuery(id), cancellationToken));
    }

    /// <summary>Creates a service (Admin).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceDto>> Create([FromBody] CreateServiceCommand command,
        CancellationToken cancellationToken)
    {
        ServiceDto created = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates a service (Admin).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> Update(int id, [FromBody] UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        UpdateServiceCommand command = new UpdateServiceCommand(id, request.Name, request.Description,
            request.DurationInMinutes, request.Price);
        return Ok(await _sender.Send(command, cancellationToken));
    }

    /// <summary>Activates or deactivates a service (Admin).</summary>
    [HttpPut("{id:int}/active")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> SetActive(int id, [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new SetServiceActiveCommand(id, request.IsActive), cancellationToken));
    }

    /// <summary>Deletes a service that has no appointments (Admin).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteServiceCommand(id), cancellationToken);
        return NoContent();
    }
}
