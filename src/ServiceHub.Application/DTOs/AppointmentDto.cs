using ServiceHub.Domain.Enums;

namespace ServiceHub.Application.DTOs;

public sealed record AppointmentDto(
    int Id,
    int ServiceId,
    string ServiceName,
    string CustomerId,
    string CustomerName,
    string? EmployeeId,
    string? EmployeeName,
    DateTime AppointmentDate,
    DateTime EndDate,
    AppointmentStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
