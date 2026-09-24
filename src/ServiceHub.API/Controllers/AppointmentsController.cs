using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub.API.Contracts;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Features.Appointments.Commands;
using ServiceHub.Application.Features.Appointments.Queries;
using ServiceHub.Domain.Constants;

namespace ServiceHub.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
[Produces("application/json")]
public class AppointmentsController : ControllerBase
{
    private readonly ISender _sender;

    public AppointmentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Books an appointment for the logged-in customer.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Create([FromBody] CreateAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        AppointmentDto created = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Lists the logged-in customer's own appointments.</summary>
    [HttpGet("my")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetMyAppointmentsQuery(), cancellationToken));
    }

    /// <summary>Gets one appointment (owner customer, assigned employee or admin).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetAppointmentByIdQuery(id), cancellationToken));
    }

    /// <summary>Cancels an appointment (owner customer, assigned employee or admin).</summary>
    [HttpPut("{id:int}/cancel")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Cancel(int id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new CancelAppointmentCommand(id), cancellationToken));
    }

    /// <summary>Changes the status to Confirmed, Completed or Cancelled (Admin or assigned Employee).</summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Employee)]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> UpdateStatus(int id, [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new UpdateAppointmentStatusCommand(id, request.Status), cancellationToken));
    }
}
