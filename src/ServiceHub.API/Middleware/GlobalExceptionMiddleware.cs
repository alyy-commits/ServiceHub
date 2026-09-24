using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Domain.Exceptions;

namespace ServiceHub.API.Middleware;

/// <summary>
/// Single place where exceptions become HTTP responses, so controllers never need try/catch.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected: nothing to send back.
            _logger.LogInformation("Request was cancelled by the client: {Path}", context.Request.Path);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        int statusCode;
        string message;
        IDictionary<string, string[]>? errors = null;

        if (exception is ValidationException validationException)
        {
            statusCode = StatusCodes.Status400BadRequest;
            message = "One or more validation errors occurred.";
            errors = GroupErrors(validationException.Errors.ToList());
        }
        else if (exception is BadRequestException)
        {
            statusCode = StatusCodes.Status400BadRequest;
            message = exception.Message;
        }
        else if (exception is UnauthorizedException)
        {
            statusCode = StatusCodes.Status401Unauthorized;
            message = exception.Message;
        }
        else if (exception is ForbiddenException)
        {
            statusCode = StatusCodes.Status403Forbidden;
            message = exception.Message;
        }
        else if (exception is NotFoundException)
        {
            statusCode = StatusCodes.Status404NotFound;
            message = exception.Message;
        }
        else if (exception is ConflictException || exception is DomainException)
        {
            statusCode = StatusCodes.Status409Conflict;
            message = exception.Message;
        }
        else if (exception is DbUpdateException)
        {
            // Typically a unique index violation (e.g. two people booking the same slot at the same moment).
            statusCode = StatusCodes.Status409Conflict;
            message = "The operation conflicts with existing data. Please refresh and try again.";
        }
        else
        {
            statusCode = StatusCodes.Status500InternalServerError;
            message = "An unexpected error occurred. Please try again later.";
        }

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", context.TraceIdentifier);
        }
        else if (exception is DbUpdateException)
        {
            _logger.LogWarning(exception, "Database conflict. TraceId: {TraceId}", context.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning("Request failed with {StatusCode}: {Message}", statusCode, exception.Message);
        }

        await ErrorResponseWriter.WriteAsync(context, statusCode, message, errors);
    }

    private static IDictionary<string, string[]> GroupErrors(List<ValidationFailure> failures)
    {
        Dictionary<string, List<string>> grouped = new Dictionary<string, List<string>>();

        for (int i = 0; i < failures.Count; i++)
        {
            string key = string.IsNullOrWhiteSpace(failures[i].PropertyName) ? "Request" : failures[i].PropertyName;
            if (!grouped.ContainsKey(key))
            {
                grouped[key] = new List<string>();
            }

            grouped[key].Add(failures[i].ErrorMessage);
        }

        Dictionary<string, string[]> result = new Dictionary<string, string[]>();
        List<string> keys = grouped.Keys.ToList();
        for (int i = 0; i < keys.Count; i++)
        {
            result[keys[i]] = grouped[keys[i]].ToArray();
        }

        return result;
    }
}
