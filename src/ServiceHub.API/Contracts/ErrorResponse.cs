namespace ServiceHub.API.Contracts;

/// <summary>Consistent error body returned by every failing request.</summary>
public sealed class ErrorResponse
{
    public ErrorResponse(int statusCode, string message, string traceId, IDictionary<string, string[]>? errors = null)
    {
        StatusCode = statusCode;
        Message = message;
        TraceId = traceId;
        Errors = errors;
    }

    public int StatusCode { get; }
    public string Message { get; }
    public string TraceId { get; }

    /// <summary>Validation errors grouped by property name (only present for 400 validation failures).</summary>
    public IDictionary<string, string[]>? Errors { get; }
}
