using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Interfaces;
using ServiceHub.Infrastructure.BackgroundJobs;
using ServiceHub.Infrastructure.Identity;
using ServiceHub.Infrastructure.Notifications;
using ServiceHub.Infrastructure.Persistence;
using ServiceHub.Infrastructure.Repositories;

namespace ServiceHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        // ----- EF Core -----
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

        // ----- Identity (core only: no cookies, JWT is configured in the API project) -----
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        // ----- JWT -----
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // ----- Repositories / Unit of Work / Services -----
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<INotificationService, LoggingNotificationService>(); // swap for an email/SMS implementation later

        // ----- Hangfire (SQL Server storage) -----
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));
        services.AddHangfireServer();

        services.AddScoped<AppointmentReminderJob>();
        services.AddScoped<AppointmentStatusUpdateJob>();

        return services;
    }
}
