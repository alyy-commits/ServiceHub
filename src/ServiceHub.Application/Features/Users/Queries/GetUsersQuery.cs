using FluentValidation;
using MediatR;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Application.DTOs;
using ServiceHub.Domain.Constants;

namespace ServiceHub.Application.Features.Users.Queries;

public sealed record GetUsersQuery(string? Role) : IRequest<IReadOnlyList<UserDto>>;

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Role)
            .Must(role => Roles.All.Contains(role!))
            .When(x => !string.IsNullOrWhiteSpace(x.Role))
            .WithMessage("Role must be one of: Admin, Employee, Customer.");
    }
}

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    private readonly IIdentityService _identityService;

    public GetUsersQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        string? role = string.IsNullOrWhiteSpace(request.Role) ? null : request.Role;
        return _identityService.GetUsersAsync(role, cancellationToken);
    }
}
