using Hangfire;
using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Enums;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Infrastructure.BackgroundJobs;

/// <summary>
/// Recurring job that keeps appointment statuses realistic once time has passed:
///  - Confirmed appointments whose end time has passed  -> Completed
///  - Pending appointments whose start time has passed (nobody confirmed them) -> Cancelled
/// </summary>
public sealed class AppointmentStatusUpdateJob
{
    private readonly IAppointmentRepository _appointments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notifications;
    private readonly ILogger<AppointmentStatusUpdateJob> _logger;

    public AppointmentStatusUpdateJob(IAppointmentRepository appointments, IUnitOfWork unitOfWork,
        INotificationService notifications, ILogger<AppointmentStatusUpdateJob> logger)
    {
        _appointments = appointments;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _logger = logger;
    }

    [DisableConcurrentExecution(300)]
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Status update job started");

        CancellationToken cancellationToken = CancellationToken.None;
        IReadOnlyList<Appointment> overdue = await _appointments.GetOverdueAppointmentsAsync(DateTime.UtcNow, cancellationToken);

        int completed = 0;
        int expired = 0;

        for (int i = 0; i < overdue.Count; i++)
        {
            Appointment appointment = overdue[i];

            if (appointment.Status == AppointmentStatus.Confirmed)
            {
                appointment.Complete();
                completed++;
                await _notifications.SendAsync(appointment.CustomerId, "Appointment completed",
                    $"Your appointment for '{appointment.Service.Name}' was marked as completed.", cancellationToken);
            }
            else if (appointment.Status == AppointmentStatus.Pending)
            {
                appointment.Cancel();
                expired++;
                await _notifications.SendAsync(appointment.CustomerId, "Appointment expired",
                    $"Your appointment for '{appointment.Service.Name}' was cancelled because it was never confirmed.",
                    cancellationToken);
            }
        }

        if (overdue.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Status update job finished. Completed: {Completed}, expired (cancelled): {Expired}",
            completed, expired);
    }
}
