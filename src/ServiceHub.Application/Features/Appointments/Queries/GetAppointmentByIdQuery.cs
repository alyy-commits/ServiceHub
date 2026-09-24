using FluentValidation;
using MediatR;
using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Mappings;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Application.Features.Appointments.Queries;

public sealed record GetAppointmentByIdQuery(int Id) : IRequest<AppointmentDto>;

public sealed class GetAppointmentByIdQueryValidator : AbstractValidator<GetAppointmentByIdQuery>
{
    public GetAppointmentByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public sealed class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, AppointmentDto>
{
    private readonly IAppointmentRepository _appointments;
    private readonly ICurrentUserService _currentUser;

    public GetAppointmentByIdQueryHandler(IAppointmentRepository appointments, ICurrentUserService currentUser)
    {
        _appointments = appointments;
        _currentUser = currentUser;
    }

    public async Task<AppointmentDto> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        Appointment appointment = await _appointments.GetByIdAsync(request.Id, cancellationToken)
                                  ?? throw new NotFoundException("Appointment", request.Id);

        AppointmentAccessPolicy.EnsureCanAccess(appointment, _currentUser);
        return appointment.ToDto();
    }
}
