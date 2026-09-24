using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Enums;

namespace ServiceHub.Domain.Interfaces;

public interface IAppointmentRepository
{
    /// <summary>Returns the appointment with Service, Customer and Employee loaded.</summary>
    Task<Appointment?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Appointment>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Appointment>> GetByEmployeeAsync(string employeeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Appointment>> GetAllAsync(AppointmentStatus? status, CancellationToken cancellationToken);

    /// <summary>
    /// True when a non-cancelled appointment overlaps [startUtc, endUtc) for the same service
    /// or (when an employee is given) for the same employee.
    /// </summary>
    Task<bool> HasConflictAsync(int serviceId, string? employeeId, DateTime startUtc, DateTime endUtc,
        int? excludeAppointmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Appointment>> GetAppointmentsNeedingReminderAsync(DateTime fromUtc, DateTime toUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Appointment>> GetOverdueAppointmentsAsync(DateTime nowUtc, CancellationToken cancellationToken);

    Task AddAsync(Appointment appointment, CancellationToken cancellationToken);
}
