using Microsoft.EntityFrameworkCore;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Interfaces;
using ServiceHub.Infrastructure.Persistence;

namespace ServiceHub.Infrastructure.Repositories;

public sealed class ServiceRepository : IServiceRepository
{
    private readonly ApplicationDbContext _context;

    public ServiceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Service?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _context.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        IQueryable<Service> query = _context.Services.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(string name, int? excludeServiceId, CancellationToken cancellationToken)
    {
        IQueryable<Service> query = _context.Services.Where(s => s.Name == name);
        if (excludeServiceId.HasValue)
        {
            int excludedId = excludeServiceId.Value;
            query = query.Where(s => s.Id != excludedId);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasAppointmentsAsync(int serviceId, CancellationToken cancellationToken)
    {
        return _context.Appointments.AnyAsync(a => a.ServiceId == serviceId, cancellationToken);
    }

    public async Task AddAsync(Service service, CancellationToken cancellationToken)
    {
        await _context.Services.AddAsync(service, cancellationToken);
    }

    public void Remove(Service service)
    {
        _context.Services.Remove(service);
    }
}
