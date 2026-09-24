using ServiceHub.Domain.Enums;

namespace ServiceHub.API.Contracts;

// Request bodies for endpoints where the id comes from the route.
// They are converted to MediatR commands inside the controllers.

/// <summary>Body of PUT /api/services/{id}.</summary>
public sealed record UpdateServiceRequest(string Name, string Description, int DurationInMinutes, decimal Price);

/// <summary>Body of PUT /api/services/{id}/active.</summary>
public sealed record SetActiveRequest(bool IsActive);

/// <summary>Body of PUT /api/appointments/{id}/status.</summary>
public sealed record UpdateStatusRequest(AppointmentStatus Status);

/// <summary>Body of PUT /api/admin/appointments/{id}/employee.</summary>
public sealed record AssignEmployeeRequest(string EmployeeId);
