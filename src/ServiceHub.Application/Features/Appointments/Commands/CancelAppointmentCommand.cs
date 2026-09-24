using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Mappings;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Application.Features.Appointments.Commands;

public sealed record CancelAppointmentCommand(int Id) : IRequest<AppointmentDto>;

public sealed class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
{
    public CancelAppointmentCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public sealed class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, AppointmentDto>
{
    private readonly IAppointmentRepository _appointments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly ILogger<CancelAppointmentCommandHandler> _logger;

    public CancelAppointmentCommandHandler(
        IAppointmentRepository appointments,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        INotificationService notifications,
        ILogger<CancelAppointmentCommandHandler> logger)
    {
        _appointments = appointments;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<AppointmentDto> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        Appointment appointment = await _appointments.GetByIdAsync(request.Id, cancellationToken)
                                  ?? throw new NotFoundException("Appointment", request.Id);

        AppointmentAccessPolicy.EnsureCanAccess(appointment, _currentUser);

        appointment.Cancel(); // Domain rule: a completed appointment cannot be cancelled.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Appointment {AppointmentId} cancelled by user {UserId}", appointment.Id, _currentUser.UserId);

        await _notifications.SendAsync(appointment.CustomerId, "Appointment cancelled",
            $"Your appointment for '{appointment.Service.Name}' on {appointment.AppointmentDate:u} was cancelled.",
            cancellationToken);

        return appointment.ToDto();
    }
}
