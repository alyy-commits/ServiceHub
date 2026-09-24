using Hangfire;
using ServiceHub.API.Auth;
using ServiceHub.API.Extensions;
using ServiceHub.API.Middleware;
using ServiceHub.Application;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Infrastructure;
using ServiceHub.Infrastructure.BackgroundJobs;
using ServiceHub.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddApiControllers();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerDocumentation();

WebApplication app = builder.Build();

// ---------- Database: apply migrations + seed data (needed before Hangfire touches the database) ----------
await DbInitializer.InitializeAsync(app.Services);

// ---------- HTTP pipeline ----------
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ServiceHub API v1");
        options.DocumentTitle = "ServiceHub API";
    });
}

app.UseHttpsRedirection();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter(app.Environment.IsDevelopment()) }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

RecurringJobsRegistrar.Register(app.Services);

app.Run();
