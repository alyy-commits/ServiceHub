using ServiceHub.Application.DTOs;

namespace ServiceHub.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<AuthResponse> RegisterCustomerAsync(string fullName, string email, string password, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken);
    Task<UserDto> CreateEmployeeAsync(string fullName, string email, string password, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserDto>> GetUsersAsync(string? role, CancellationToken cancellationToken);
    Task<bool> IsActiveUserInRoleAsync(string userId, string role, CancellationToken cancellationToken);
    Task SetUserActiveAsync(string userId, bool isActive, CancellationToken cancellationToken);
}
