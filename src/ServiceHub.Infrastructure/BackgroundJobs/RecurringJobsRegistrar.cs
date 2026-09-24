using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceHub.Infrastructure.BackgroundJobs;

public static class RecurringJobsRegistrar
{
    public const string ReminderJobId = "appointment-reminders";
    public const string StatusJobId = "appointment-status-update";

    /// <summary>Creates or updates the recurring jobs. Call once after the app is built.</summary>
    public static void Register(IServiceProvider serviceProvider)
    {
        IRecurringJobManager manager = serviceProvider.GetRequiredService<IRecurringJobManager>();

        manager.AddOrUpdate<AppointmentReminderJob>(ReminderJobId, job => job.ExecuteAsync(), "*/15 * * * *");
        manager.AddOrUpdate<AppointmentStatusUpdateJob>(StatusJobId, job => job.ExecuteAsync(), "*/5 * * * *");
    }
}
