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

public sealed record AssignEmployeeCommand(int AppointmentId, string EmployeeId) : IRequest<AppointmentDto>;

public sealed class AssignEmployeeCommandValidator : AbstractValidator<AssignEmployeeCommand>
{
    public AssignEmployeeCommandValidator()
    {
        RuleFor(x => x.AppointmentId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}

public sealed class AssignEmployeeCommandHandler : IRequestHandler<AssignEmployeeCommand, AppointmentDto>
{
    private readonly IAppointmentRepository _appointments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly ILogger<AssignEmployeeCommandHandler> _logger;

    public AssignEmployeeCommandHandler(
        IAppointmentRepository appointments,
        IUnitOfWork unitOfWork,
        IIdentityService identityService,
        ILogger<AssignEmployeeCommandHandler> logger)
    {
        _appointments = appointments;
        _unitOfWork = unitOfWork;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<AppointmentDto> Handle(AssignEmployeeCommand request, CancellationToken cancellationToken)
    {
        Appointment appointment = await _appointments.GetByIdAsync(request.AppointmentId, cancellationToken)
                                  ?? throw new NotFoundException("Appointment", request.AppointmentId);

        if (!await _identityService.IsActiveUserInRoleAsync(request.EmployeeId, Roles.Employee, cancellationToken))
        {
            throw new BadRequestException("The selected employee does not exist or is not active.");
        }

        bool busy = await _appointments.HasConflictAsync(appointment.ServiceId, request.EmployeeId,
            appointment.AppointmentDate, appointment.EndDate, appointment.Id, cancellationToken);
        if (busy)
        {
            throw new ConflictException("The employee already has an appointment in that time slot.");
        }

        appointment.AssignEmployee(request.EmployeeId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee {EmployeeId} assigned to appointment {AppointmentId}",
            request.EmployeeId, appointment.Id);

        // Reload so the Employee navigation property is populated for the DTO.
        Appointment updated = await _appointments.GetByIdAsync(appointment.Id, cancellationToken)
                              ?? throw new NotFoundException("Appointment", appointment.Id);
        return updated.ToDto();
    }
}
