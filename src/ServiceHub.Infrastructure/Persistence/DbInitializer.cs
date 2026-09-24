using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceHub.Domain.Constants;
using ServiceHub.Domain.Entities;

namespace ServiceHub.Infrastructure.Persistence;

/// <summary>Applies pending migrations and seeds roles, demo users and demo services.</summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();

        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        IConfiguration configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ServiceHub.DbInitializer");

        await context.Database.MigrateAsync(cancellationToken);

        // 1) Roles
        for (int i = 0; i < Roles.All.Length; i++)
        {
            string role = Roles.All[i];
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Role {Role} created", role);
            }
        }

        // 2) Demo users (passwords come from configuration: Seed section - never hard-coded here)
        await SeedUserAsync(userManager, logger, "System Admin",
            configuration["Seed:AdminEmail"] ?? "admin@servicehub.com", configuration["Seed:AdminPassword"], Roles.Admin);
        await SeedUserAsync(userManager, logger, "Demo Employee",
            configuration["Seed:EmployeeEmail"] ?? "employee@servicehub.com", configuration["Seed:EmployeePassword"], Roles.Employee);
        await SeedUserAsync(userManager, logger, "Demo Customer",
            configuration["Seed:CustomerEmail"] ?? "customer@servicehub.com", configuration["Seed:CustomerPassword"], Roles.Customer);

        // 3) Demo services
        if (!await context.Services.AnyAsync(cancellationToken))
        {
            Service[] services =
            {
                new Service("Haircut", "Classic haircut and styling.", 30, 25m),
                new Service("Full Body Massage", "Relaxing 60 minute full body massage.", 60, 60m),
                new Service("Dental Cleaning", "Professional dental cleaning and check-up.", 45, 80m),
                new Service("Car Oil Change", "Engine oil and filter replacement.", 30, 45m),
                new Service("Business Consultation", "Short one-to-one consultation session.", 20, 15m)
            };

            for (int i = 0; i < services.Length; i++)
            {
                context.Services.Add(services[i]);
            }

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} demo services", services.Length);
        }
    }

    private static async Task SeedUserAsync(UserManager<ApplicationUser> userManager, ILogger logger,
        string fullName, string email, string? password, string role)
    {
        if (await userManager.FindByEmailAsync(email) != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Seed password for {Email} is not configured. User was not created.", email);
            return;
        }

        ApplicationUser user = new ApplicationUser(fullName, email);
        IdentityResult created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            logger.LogError("Could not seed user {Email}: {Errors}", email,
                string.Join("; ", created.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, role);
        logger.LogInformation("Seeded {Role} user {Email}", role, email);
    }
}
