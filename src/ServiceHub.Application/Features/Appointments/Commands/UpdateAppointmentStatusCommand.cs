using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Mappings;
using ServiceHub.Domain.Constants;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Enums;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Application.Features.Appointments.Commands;

public sealed record UpdateAppointmentStatusCommand(int Id, AppointmentStatus Status) : IRequest<AppointmentDto>;

public sealed class UpdateAppointmentStatusCommandValidator : AbstractValidator<UpdateAppointmentStatusCommand>
{
    public UpdateAppointmentStatusCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Status)
            .NotEqual(AppointmentStatus.Pending)
            .WithMessage("Status can only be changed to Confirmed, Completed or Cancelled.");
    }
}

public sealed class UpdateAppointmentStatusCommandHandler : IRequestHandler<UpdateAppointmentStatusCommand, AppointmentDto>
{
    private readonly IAppointmentRepository _appointments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly ILogger<UpdateAppointmentStatusCommandHandler> _logger;

    public UpdateAppointmentStatusCommandHandler(
        IAppointmentRepository appointments,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        INotificationService notifications,
        ILogger<UpdateAppointmentStatusCommandHandler> logger)
    {
        _appointments = appointments;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<AppointmentDto> Handle(UpdateAppointmentStatusCommand request, CancellationToken cancellationToken)
    {
        Appointment appointment = await _appointments.GetByIdAsync(request.Id, cancellationToken)
                                  ?? throw new NotFoundException("Appointment", request.Id);

        // Only admins, or the employee assigned to this appointment, may change its status.
        bool isAdmin = _currentUser.IsInRole(Roles.Admin);
        bool isAssignedEmployee = _currentUser.IsInRole(Roles.Employee)
                                  && appointment.EmployeeId != null
                                  && appointment.EmployeeId == _currentUser.UserId;
        if (!isAdmin && !isAssignedEmployee)
        {
            throw new ForbiddenException("Only an administrator or the assigned employee can change the status.");
        }

        AppointmentStatus oldStatus = appointment.Status;
        appointment.ChangeStatus(request.Status); // Domain rules are enforced here.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Appointment {AppointmentId} status changed from {OldStatus} to {NewStatus} by user {UserId}",
            appointment.Id, oldStatus, appointment.Status, _currentUser.UserId);

        await _notifications.SendAsync(appointment.CustomerId, "Appointment status updated",
            $"Your appointment for '{appointment.Service.Name}' is now {appointment.Status}.",
            cancellationToken);

        return appointment.ToDto();
    }
}
