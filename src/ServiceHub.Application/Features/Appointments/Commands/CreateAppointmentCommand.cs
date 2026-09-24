using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Mappings;
using ServiceHub.Domain.Constants;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Application.Features.Appointments.Commands;

/// <param name="EmployeeId">Optional. Leave null to let an administrator assign an employee later.</param>
public sealed record CreateAppointmentCommand(int ServiceId, DateTimeOffset AppointmentDate, string? EmployeeId)
    : IRequest<AppointmentDto>;

public sealed class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.ServiceId).GreaterThan(0);

        RuleFor(x => x.AppointmentDate)
            .Must(date => date != default)
            .WithMessage("A valid appointment date and time is required.")
            .Must(date => date > DateTimeOffset.UtcNow)
            .WithMessage("The appointment date must be in the future.")
            .Must(date => date <= DateTimeOffset.UtcNow.AddYears(1))
            .WithMessage("Appointments cannot be booked more than one year ahead.");
    }
}

public sealed class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, AppointmentDto>
{
    private readonly IServiceRepository _services;
    private readonly IAppointmentRepository _appointments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly ILogger<CreateAppointmentCommandHandler> _logger;

    public CreateAppointmentCommandHandler(
        IServiceRepository services,
        IAppointmentRepository appointments,
        IUnitOfWork unitOfWork,
        IIdentityService identityService,
        ICurrentUserService currentUser,
        INotificationService notifications,
        ILogger<CreateAppointmentCommandHandler> logger)
    {
        _services = services;
        _appointments = appointments;
        _unitOfWork = unitOfWork;
        _identityService = identityService;
        _currentUser = currentUser;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<AppointmentDto> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        string customerId = _currentUser.UserId ?? throw new UnauthorizedException();

        Service service = await _services.GetByIdAsync(request.ServiceId, cancellationToken)
                          ?? throw new NotFoundException("Service", request.ServiceId);

        if (!service.IsActive)
        {
            throw new BadRequestException("This service is currently inactive and cannot be booked.");
        }

        string? employeeId = string.IsNullOrWhiteSpace(request.EmployeeId) ? null : request.EmployeeId;
        if (employeeId != null &&
            !await _identityService.IsActiveUserInRoleAsync(employeeId, Roles.Employee, cancellationToken))
        {
            throw new BadRequestException("The selected employee does not exist or is not active.");
        }

        DateTime startUtc = request.AppointmentDate.UtcDateTime;
        DateTime endUtc = startUtc.AddMinutes(service.DurationInMinutes);

        if (await _appointments.HasConflictAsync(service.Id, employeeId, startUtc, endUtc, null, cancellationToken))
        {
            throw new ConflictException("The selected time slot is already booked. Please choose another time.");
        }

        Appointment appointment = new Appointment(customerId, service.Id, startUtc, service.DurationInMinutes, employeeId);
        await _appointments.AddAsync(appointment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Appointment {AppointmentId} created by customer {CustomerId} for service {ServiceId}",
            appointment.Id, customerId, service.Id);

        await _notifications.SendAsync(customerId, "Appointment received",
            $"Your appointment for '{service.Name}' on {startUtc:u} was created and is pending confirmation.",
            cancellationToken);

        // Reload so Customer / Service / Employee navigation properties are populated for the DTO.
        Appointment created = await _appointments.GetByIdAsync(appointment.Id, cancellationToken)
                              ?? throw new NotFoundException("Appointment", appointment.Id);
        return created.ToDto();
    }
}
