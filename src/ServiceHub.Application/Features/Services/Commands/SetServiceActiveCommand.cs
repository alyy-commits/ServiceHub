using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Mappings;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Application.Features.Services.Commands;

public sealed record SetServiceActiveCommand(int Id, bool IsActive) : IRequest<ServiceDto>;

public sealed class SetServiceActiveCommandValidator : AbstractValidator<SetServiceActiveCommand>
{
    public SetServiceActiveCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public sealed class SetServiceActiveCommandHandler : IRequestHandler<SetServiceActiveCommand, ServiceDto>
{
    private readonly IServiceRepository _services;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetServiceActiveCommandHandler> _logger;

    public SetServiceActiveCommandHandler(IServiceRepository services, IUnitOfWork unitOfWork,
        ILogger<SetServiceActiveCommandHandler> logger)
    {
        _services = services;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceDto> Handle(SetServiceActiveCommand request, CancellationToken cancellationToken)
    {
        Service service = await _services.GetByIdAsync(request.Id, cancellationToken)
                          ?? throw new NotFoundException("Service", request.Id);

        service.SetActive(request.IsActive);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Service {ServiceId} active status set to {IsActive}", service.Id, service.IsActive);
        return service.ToDto();
    }
}
