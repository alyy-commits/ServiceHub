using Hangfire;
using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Infrastructure.BackgroundJobs;

/// <summary>Recurring job: sends a reminder for appointments starting within the next 24 hours.</summary>
public sealed class AppointmentReminderJob
{
    private readonly IAppointmentRepository _appointments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notifications;
    private readonly ILogger<AppointmentReminderJob> _logger;

    public AppointmentReminderJob(IAppointmentRepository appointments, IUnitOfWork unitOfWork,
        INotificationService notifications, ILogger<AppointmentReminderJob> logger)
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
        _logger.LogInformation("Reminder job started");

        CancellationToken cancellationToken = CancellationToken.None;
        DateTime now = DateTime.UtcNow;

        IReadOnlyList<Appointment> due = await _appointments.GetAppointmentsNeedingReminderAsync(
            now, now.AddHours(24), cancellationToken);

        for (int i = 0; i < due.Count; i++)
        {
            Appointment appointment = due[i];
            await _notifications.SendAsync(appointment.CustomerId, "Appointment reminder",
                $"Reminder: your appointment for '{appointment.Service.Name}' starts at {appointment.AppointmentDate:u}.",
                cancellationToken);
            appointment.MarkReminderSent();
        }

        if (due.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Reminder job finished. Reminders sent: {Count}", due.Count);
    }
}
