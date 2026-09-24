using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ServiceHub.Application.Behaviors;

/// <summary>
/// Logs the request name and duration. The request payload is intentionally NOT logged
/// because commands such as Register/Login contain passwords.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        Stopwatch stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Handling {RequestName}", requestName);
        TResponse response = await next();
        stopwatch.Stop();
        _logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds} ms", requestName, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
