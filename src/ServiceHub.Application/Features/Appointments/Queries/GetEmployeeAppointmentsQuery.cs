using MediatR;
using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Mappings;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Application.Features.Appointments.Queries;

public sealed record GetEmployeeAppointmentsQuery : IRequest<IReadOnlyList<AppointmentDto>>;

public sealed class GetEmployeeAppointmentsQueryHandler
    : IRequestHandler<GetEmployeeAppointmentsQuery, IReadOnlyList<AppointmentDto>>
{
    private readonly IAppointmentRepository _appointments;
    private readonly ICurrentUserService _currentUser;

    public GetEmployeeAppointmentsQueryHandler(IAppointmentRepository appointments, ICurrentUserService currentUser)
    {
        _appointments = appointments;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AppointmentDto>> Handle(GetEmployeeAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        string userId = _currentUser.UserId ?? throw new UnauthorizedException();
        IReadOnlyList<Appointment> items = await _appointments.GetByEmployeeAsync(userId, cancellationToken);
        return items.ToDtoList();
    }
}
