using MediatR;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Mappings;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Enums;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Application.Features.Appointments.Queries;

public sealed record GetAllAppointmentsQuery(AppointmentStatus? Status) : IRequest<IReadOnlyList<AppointmentDto>>;

public sealed class GetAllAppointmentsQueryHandler : IRequestHandler<GetAllAppointmentsQuery, IReadOnlyList<AppointmentDto>>
{
    private readonly IAppointmentRepository _appointments;

    public GetAllAppointmentsQueryHandler(IAppointmentRepository appointments)
    {
        _appointments = appointments;
    }

    public async Task<IReadOnlyList<AppointmentDto>> Handle(GetAllAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Appointment> items = await _appointments.GetAllAsync(request.Status, cancellationToken);
        return items.ToDtoList();
    }
}
