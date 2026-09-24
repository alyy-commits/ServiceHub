using ServiceHub.Application.DTOs;
using ServiceHub.Domain.Entities;

namespace ServiceHub.Application.Mappings;

public static class MappingExtensions
{
    public static ServiceDto ToDto(this Service service)
    {
        return new ServiceDto(
            service.Id,
            service.Name,
            service.Description,
            service.DurationInMinutes,
            service.Price,
            service.IsActive,
            AsUtc(service.CreatedAt));
    }

    public static AppointmentDto ToDto(this Appointment appointment)
    {
        return new AppointmentDto(
            appointment.Id,
            appointment.ServiceId,
            appointment.Service.Name,
            appointment.CustomerId,
            appointment.Customer.FullName,
            appointment.EmployeeId,
            appointment.Employee?.FullName,
            AsUtc(appointment.AppointmentDate),
            AsUtc(appointment.EndDate),
            appointment.Status,
            AsUtc(appointment.CreatedAt),
            appointment.UpdatedAt.HasValue ? AsUtc(appointment.UpdatedAt.Value) : null);
    }

    public static IReadOnlyList<AppointmentDto> ToDtoList(this IReadOnlyList<Appointment> appointments)
    {
        List<AppointmentDto> result = new List<AppointmentDto>();
        for (int i = 0; i < appointments.Count; i++)
        {
            result.Add(appointments[i].ToDto());
        }

        return result;
    }

    // SQL Server returns DateTime with Kind = Unspecified. All dates are stored in UTC,
    // so mark them as UTC to make the JSON output end with "Z".
    public static DateTime AsUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
