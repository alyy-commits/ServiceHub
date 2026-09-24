using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Mappings;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Application.Features.Services.Commands;

public sealed record CreateServiceCommand(string Name, string Description, int DurationInMinutes, decimal Price)
    : IRequest<ServiceDto>;

public sealed class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DurationInMinutes).InclusiveBetween(5, 480)
            .WithMessage("Duration must be between 5 and 480 minutes.");
        RuleFor(x => x.Price).GreaterThan(0).LessThanOrEqualTo(100000);
    }
}

public sealed class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, ServiceDto>
{
    private readonly IServiceRepository _services;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateServiceCommandHandler> _logger;

    public CreateServiceCommandHandler(IServiceRepository services, IUnitOfWork unitOfWork,
        ILogger<CreateServiceCommandHandler> logger)
    {
        _services = services;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        string name = request.Name.Trim();
        if (await _services.NameExistsAsync(name, null, cancellationToken))
        {
            throw new ConflictException($"A service named '{name}' already exists.");
        }

        Service service = new Service(name, request.Description.Trim(), request.DurationInMinutes, request.Price);
        await _services.AddAsync(service, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Service {ServiceId} created", service.Id);
        return service.ToDto();
    }
}
