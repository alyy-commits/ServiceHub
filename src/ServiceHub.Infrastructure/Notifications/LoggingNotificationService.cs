using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Interfaces;

namespace ServiceHub.Infrastructure.Notifications;

/// <summary>
/// Simple INotificationService that only writes to the log.
/// To send real emails/SMS later, create e.g. EmailNotificationService : INotificationService
/// (using SMTP / SendGrid / Twilio) and change ONE line in Infrastructure DependencyInjection.
/// </summary>
public sealed class LoggingNotificationService : INotificationService
{
    private readonly ILogger<LoggingNotificationService> _logger;

    public LoggingNotificationService(ILogger<LoggingNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string userId, string subject, string message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("NOTIFICATION to user {UserId} | {Subject} | {Message}", userId, subject, message);
        return Task.CompletedTask;
    }
}
