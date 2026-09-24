using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ServiceHub.API.Contracts;

namespace ServiceHub.API.Extensions;

public static class ControllerExtensions
{
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                // Enums are sent/received as text: "Confirmed" instead of 1.
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                // Malformed JSON / wrong types are rejected by model binding before MediatR runs.
                // Return them in the same ErrorResponse shape used everywhere else.
                options.InvalidModelStateResponseFactory = context =>
                {
                    Dictionary<string, string[]> errors = new Dictionary<string, string[]>();
                    List<string> keys = context.ModelState.Keys.ToList();

                    for (int i = 0; i < keys.Count; i++)
                    {
                        ModelStateEntry? entry = context.ModelState[keys[i]];
                        if (entry == null || entry.Errors.Count == 0)
                        {
                            continue;
                        }

                        List<string> messages = new List<string>();
                        for (int j = 0; j < entry.Errors.Count; j++)
                        {
                            string text = string.IsNullOrWhiteSpace(entry.Errors[j].ErrorMessage)
                                ? "The value is invalid."
                                : entry.Errors[j].ErrorMessage;
                            messages.Add(text);
                        }

                        errors[string.IsNullOrWhiteSpace(keys[i]) ? "Request" : keys[i]] = messages.ToArray();
                    }

                    ErrorResponse body = new ErrorResponse(StatusCodes.Status400BadRequest,
                        "One or more validation errors occurred.", context.HttpContext.TraceIdentifier, errors);
                    return new BadRequestObjectResult(body);
                };
            });

        return services;
    }
}
