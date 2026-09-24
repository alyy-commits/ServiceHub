using FluentValidation;
using MediatR;
using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Mappings;
using ServiceHub.Domain.Constants;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Application.Features.Services.Queries;

public sealed record GetServiceByIdQuery(int Id) : IRequest<ServiceDto>;

public sealed class GetServiceByIdQueryValidator : AbstractValidator<GetServiceByIdQuery>
{
    public GetServiceByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public sealed class GetServiceByIdQueryHandler : IRequestHandler<GetServiceByIdQuery, ServiceDto>
{
    private readonly IServiceRepository _services;
    private readonly ICurrentUserService _currentUser;

    public GetServiceByIdQueryHandler(IServiceRepository services, ICurrentUserService currentUser)
    {
        _services = services;
        _currentUser = currentUser;
    }

    public async Task<ServiceDto> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
    {
        Service service = await _services.GetByIdAsync(request.Id, cancellationToken)
                          ?? throw new NotFoundException("Service", request.Id);

        // Inactive services are hidden from everyone except administrators.
        if (!service.IsActive && !_currentUser.IsInRole(Roles.Admin))
        {
            throw new NotFoundException("Service", request.Id);
        }

        return service.ToDto();
    }
}
