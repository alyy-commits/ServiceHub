using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Application.DTOs;
using ServiceHub.Domain.Constants;
using ServiceHub.Domain.Entities;

namespace ServiceHub.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(UserManager<ApplicationUser> userManager, IJwtTokenGenerator jwtTokenGenerator,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterCustomerAsync(string fullName, string email, string password,
        CancellationToken cancellationToken)
    {
        ApplicationUser user = await CreateUserAsync(fullName, email, password, Roles.Customer);
        return await BuildAuthResponseAsync(user);
    }

    public async Task<UserDto> CreateEmployeeAsync(string fullName, string email, string password,
        CancellationToken cancellationToken)
    {
        ApplicationUser user = await CreateUserAsync(fullName, email, password, Roles.Employee);
        return await ToUserDtoAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByEmailAsync(email);

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Login failed: unknown or inactive account for {Email}", email);
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login blocked: account {UserId} is locked out", user.Id);
            throw new UnauthorizedException("The account is temporarily locked because of too many failed attempts.");
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            await _userManager.AccessFailedAsync(user);
            _logger.LogWarning("Login failed: wrong password for account {UserId}", user.Id);
            throw new UnauthorizedException("Invalid email or password.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return await BuildAuthResponseAsync(user);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(string? role, CancellationToken cancellationToken)
    {
        IList<ApplicationUser> users;
        if (role == null)
        {
            users = await _userManager.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync(cancellationToken);
        }
        else
        {
            users = await _userManager.GetUsersInRoleAsync(role);
        }

        List<UserDto> result = new List<UserDto>();
        for (int i = 0; i < users.Count; i++)
        {
            result.Add(await ToUserDtoAsync(users[i]));
        }

        return result;
    }

    public async Task<bool> IsActiveUserInRoleAsync(string userId, string role, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);
        return user != null && user.IsActive && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task SetUserActiveAsync(string userId, bool isActive, CancellationToken cancellationToken)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(userId)
                               ?? throw new NotFoundException("User", userId);

        user.SetActive(isActive);
        IdentityResult result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw ToValidationException(result);
        }
    }

    // ---------------- helpers ----------------

    private async Task<ApplicationUser> CreateUserAsync(string fullName, string email, string password, string role)
    {
        if (await _userManager.FindByEmailAsync(email) != null)
        {
            throw new ConflictException("A user with this email already exists.");
        }

        ApplicationUser user = new ApplicationUser(fullName, email);

        IdentityResult createResult = await _userManager.CreateAsync(user, password); // password is hashed by Identity
        if (!createResult.Succeeded)
        {
            throw ToValidationException(createResult);
        }

        IdentityResult roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user); // do not leave a user without a role
            throw ToValidationException(roleResult);
        }

        return user;
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user)
    {
        IList<string> roles = await _userManager.GetRolesAsync(user);
        (string token, DateTime expiresAtUtc) = _jwtTokenGenerator.Generate(user, roles);

        return new AuthResponse(token, expiresAtUtc, user.Id, user.FullName, user.Email ?? string.Empty, roles.ToList());
    }

    private async Task<UserDto> ToUserDtoAsync(ApplicationUser user)
    {
        IList<string> roles = await _userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.FullName, user.Email ?? string.Empty, roles.ToList(), user.IsActive,
            DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc));
    }

    private static ValidationException ToValidationException(IdentityResult result)
    {
        List<ValidationFailure> failures = new List<ValidationFailure>();
        List<IdentityError> errors = result.Errors.ToList();
        for (int i = 0; i < errors.Count; i++)
        {
            failures.Add(new ValidationFailure(errors[i].Code, errors[i].Description));
        }

        return new ValidationException(failures);
    }
}
