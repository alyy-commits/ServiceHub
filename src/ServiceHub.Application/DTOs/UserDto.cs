namespace ServiceHub.Application.DTOs;

public sealed record UserDto(
    string Id,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles,
    bool IsActive,
    DateTime CreatedAt);
