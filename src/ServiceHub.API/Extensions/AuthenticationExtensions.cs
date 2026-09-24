using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ServiceHub.API.Middleware;
using ServiceHub.Infrastructure.Identity;

namespace ServiceHub.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        JwtSettings jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                          ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");

        if (string.IsNullOrWhiteSpace(jwt.SecretKey) || jwt.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SecretKey must be at least 32 characters long.");
        }

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // keep short claim names: sub, role, name...
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };

                // Return the same JSON error shape for 401 / 403 coming from the auth pipeline.
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await ErrorResponseWriter.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
                            "Authentication is required. Provide a valid Bearer token.");
                    },
                    OnForbidden = context => ErrorResponseWriter.WriteAsync(context.HttpContext,
                        StatusCodes.Status403Forbidden, "You do not have permission to access this resource.")
                };
            });

        services.AddAuthorization();
        return services;
    }
}
