namespace ServiceHub.Application.Common.Interfaces;

/// <summary>
/// Abstraction for notifying a user. The current implementation only logs.
/// Replace it with an email / SMS implementation without touching the handlers or jobs.
/// </summary>
public interface INotificationService
{
    Task SendAsync(string userId, string subject, string message, CancellationToken cancellationToken);
}
