using ServiceHub.Domain.Entities;

namespace ServiceHub.Domain.Interfaces;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Service>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(string name, int? excludeServiceId, CancellationToken cancellationToken);
    Task<bool> HasAppointmentsAsync(int serviceId, CancellationToken cancellationToken);
    Task AddAsync(Service service, CancellationToken cancellationToken);
    void Remove(Service service);
}
