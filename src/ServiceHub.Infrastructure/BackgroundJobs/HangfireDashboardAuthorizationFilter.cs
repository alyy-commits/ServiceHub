using Hangfire.Dashboard;

namespace ServiceHub.Infrastructure.BackgroundJobs;

/// <summary>
/// The Hangfire dashboard cannot send a JWT header, so access is simply enabled in Development
/// and blocked everywhere else. For production, put the dashboard behind cookie/SSO auth or a VPN.
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly bool _allowAccess;

    public HangfireDashboardAuthorizationFilter(bool allowAccess)
    {
        _allowAccess = allowAccess;
    }

    public bool Authorize(DashboardContext context)
    {
        return _allowAccess;
    }
}
