namespace ServiceHub.Application.DTOs;

public sealed record ServiceDto(
    int Id,
    string Name,
    string Description,
    int DurationInMinutes,
    decimal Price,
    bool IsActive,
    DateTime CreatedAt);
