using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub.API.Contracts;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Features.Appointments.Commands;
using ServiceHub.Application.Features.Appointments.Queries;
using ServiceHub.Application.Features.Users.Commands;
using ServiceHub.Application.Features.Users.Queries;
using ServiceHub.Domain.Constants;
using ServiceHub.Domain.Enums;

namespace ServiceHub.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly ISender _sender;

    public AdminController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lists all appointments, optionally filtered by status.</summary>
    [HttpGet("appointments")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetAppointments([FromQuery] AppointmentStatus? status,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetAllAppointmentsQuery(status), cancellationToken));
    }

    /// <summary>Assigns an employee to an appointment.</summary>
    [HttpPut("appointments/{id:int}/employee")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> AssignEmployee(int id, [FromBody] AssignEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new AssignEmployeeCommand(id, request.EmployeeId), cancellationToken));
    }

    /// <summary>Lists users, optionally filtered by role (Admin, Employee, Customer).</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsers([FromQuery] string? role,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetUsersQuery(role), cancellationToken));
    }

    /// <summary>Creates an employee account.</summary>
    [HttpPost("employees")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> CreateEmployee([FromBody] CreateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        UserDto created = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Activates or deactivates a user account.</summary>
    [HttpPut("users/{id}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetUserActive(string id, [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new SetUserActiveCommand(id, request.IsActive), cancellationToken);
        return NoContent();
    }
}
