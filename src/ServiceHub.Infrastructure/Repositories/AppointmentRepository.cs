using Microsoft.EntityFrameworkCore;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Enums;
using ServiceHub.Domain.Interfaces;
using ServiceHub.Infrastructure.Persistence;

namespace ServiceHub.Infrastructure.Repositories;

public sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly ApplicationDbContext _context;

    public AppointmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private IQueryable<Appointment> QueryWithDetails()
    {
        return _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Customer)
            .Include(a => a.Employee);
    }

    public Task<Appointment?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return QueryWithDetails().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken)
    {
        return await QueryWithDetails().AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByEmployeeAsync(string employeeId, CancellationToken cancellationToken)
    {
        return await QueryWithDetails().AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetAllAsync(AppointmentStatus? status, CancellationToken cancellationToken)
    {
        IQueryable<Appointment> query = QueryWithDetails().AsNoTracking();
        if (status.HasValue)
        {
            AppointmentStatus wanted = status.Value;
            query = query.Where(a => a.Status == wanted);
        }

        return await query.OrderByDescending(a => a.AppointmentDate).ToListAsync(cancellationToken);
    }

    public Task<bool> HasConflictAsync(int serviceId, string? employeeId, DateTime startUtc, DateTime endUtc,
        int? excludeAppointmentId, CancellationToken cancellationToken)
    {
        // Two intervals overlap when: existing.Start < newEnd AND newStart < existing.End
        IQueryable<Appointment> query = _context.Appointments.Where(a =>
            a.Status != AppointmentStatus.Cancelled &&
            a.AppointmentDate < endUtc &&
            startUtc < a.EndDate &&
            (a.ServiceId == serviceId || (employeeId != null && a.EmployeeId == employeeId)));

        if (excludeAppointmentId.HasValue)
        {
            int excludedId = excludeAppointmentId.Value;
            query = query.Where(a => a.Id != excludedId);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetAppointmentsNeedingReminderAsync(DateTime fromUtc, DateTime toUtc,
        CancellationToken cancellationToken)
    {
        return await QueryWithDetails()
            .Where(a => !a.ReminderSent
                        && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
                        && a.AppointmentDate > fromUtc
                        && a.AppointmentDate <= toUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetOverdueAppointmentsAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        return await QueryWithDetails()
            .Where(a => (a.Status == AppointmentStatus.Confirmed && a.EndDate <= nowUtc)
                        || (a.Status == AppointmentStatus.Pending && a.AppointmentDate <= nowUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        await _context.Appointments.AddAsync(appointment, cancellationToken);
    }
}
