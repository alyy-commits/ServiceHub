using MediatR;
using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Mappings;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Application.Features.Appointments.Queries;

public sealed record GetMyAppointmentsQuery : IRequest<IReadOnlyList<AppointmentDto>>;

public sealed class GetMyAppointmentsQueryHandler : IRequestHandler<GetMyAppointmentsQuery, IReadOnlyList<AppointmentDto>>
{
    private readonly IAppointmentRepository _appointments;
    private readonly ICurrentUserService _currentUser;

    public GetMyAppointmentsQueryHandler(IAppointmentRepository appointments, ICurrentUserService currentUser)
    {
        _appointments = appointments;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AppointmentDto>> Handle(GetMyAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        string userId = _currentUser.UserId ?? throw new UnauthorizedException();
        IReadOnlyList<Appointment> items = await _appointments.GetByCustomerAsync(userId, cancellationToken);
        return items.ToDtoList();
    }
}
