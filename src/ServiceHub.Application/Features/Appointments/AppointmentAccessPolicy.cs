using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Domain.Constants;
using ServiceHub.Domain.Entities;

namespace ServiceHub.Application.Features.Appointments;

/// <summary>
/// Object-level authorization: role attributes on controllers say WHO may call an endpoint,
/// this policy says which appointments that caller may touch.
/// </summary>
public static class AppointmentAccessPolicy
{
    public static void EnsureCanAccess(Appointment appointment, ICurrentUserService currentUser)
    {
        if (currentUser.IsInRole(Roles.Admin))
        {
            return;
        }

        string? userId = currentUser.UserId;
        if (userId == null)
        {
            throw new UnauthorizedException();
        }

        bool isOwner = currentUser.IsInRole(Roles.Customer) && appointment.CustomerId == userId;
        bool isAssignedEmployee = currentUser.IsInRole(Roles.Employee) && appointment.EmployeeId == userId;

        if (!isOwner && !isAssignedEmployee)
        {
            throw new ForbiddenException("You do not have access to this appointment.");
        }
    }
}
