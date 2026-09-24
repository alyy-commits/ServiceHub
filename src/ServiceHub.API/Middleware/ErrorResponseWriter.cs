using System.Text.Json;
using System.Text.Json.Serialization;
using ServiceHub.API.Contracts;

namespace ServiceHub.API.Middleware;

public static class ErrorResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task WriteAsync(HttpContext context, int statusCode, string message,
        IDictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        ErrorResponse body = new ErrorResponse(statusCode, message, context.TraceIdentifier, errors);
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
