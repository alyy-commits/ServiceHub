using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Features.Appointments.Queries;
using ServiceHub.Domain.Constants;

namespace ServiceHub.API.Controllers;

[ApiController]
[Route("api/employee")]
[Authorize(Roles = Roles.Employee)]
[Produces("application/json")]
public class EmployeeController : ControllerBase
{
    private readonly ISender _sender;

    public EmployeeController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lists the appointments assigned to the logged-in employee.</summary>
    [HttpGet("appointments")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetAssignedAppointments(
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetEmployeeAppointmentsQuery(), cancellationToken));
    }
}
