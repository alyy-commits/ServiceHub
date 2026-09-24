using MediatR;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Mappings;
using ServiceHub.Domain.Constants;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Application.Features.Services.Queries;

/// <param name="IncludeInactive">Only honoured for administrators.</param>
public sealed record GetAvailableServicesQuery(bool IncludeInactive) : IRequest<IReadOnlyList<ServiceDto>>;

public sealed class GetAvailableServicesQueryHandler : IRequestHandler<GetAvailableServicesQuery, IReadOnlyList<ServiceDto>>
{
    private readonly IServiceRepository _services;
    private readonly ICurrentUserService _currentUser;

    public GetAvailableServicesQueryHandler(IServiceRepository services, ICurrentUserService currentUser)
    {
        _services = services;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ServiceDto>> Handle(GetAvailableServicesQuery request,
        CancellationToken cancellationToken)
    {
        bool includeInactive = request.IncludeInactive && _currentUser.IsInRole(Roles.Admin);
        IReadOnlyList<Service> services = await _services.GetAllAsync(includeInactive, cancellationToken);

        List<ServiceDto> result = new List<ServiceDto>();
        for (int i = 0; i < services.Count; i++)
        {
            result.Add(services[i].ToDto());
        }

        return result;
    }
}
