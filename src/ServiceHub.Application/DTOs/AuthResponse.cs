namespace ServiceHub.Application.DTOs;

public sealed record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    string UserId,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles);
