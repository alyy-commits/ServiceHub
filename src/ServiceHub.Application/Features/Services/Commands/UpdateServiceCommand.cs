using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Application.DTOs;
using ServiceHub.Application.Mappings;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Application.Features.Services.Commands;

public sealed record UpdateServiceCommand(int Id, string Name, string Description, int DurationInMinutes, decimal Price)
    : IRequest<ServiceDto>;

public sealed class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DurationInMinutes).InclusiveBetween(5, 480)
            .WithMessage("Duration must be between 5 and 480 minutes.");
        RuleFor(x => x.Price).GreaterThan(0).LessThanOrEqualTo(100000);
    }
}

public sealed class UpdateServiceCommandHandler : IRequestHandler<UpdateServiceCommand, ServiceDto>
{
    private readonly IServiceRepository _services;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateServiceCommandHandler> _logger;

    public UpdateServiceCommandHandler(IServiceRepository services, IUnitOfWork unitOfWork,
        ILogger<UpdateServiceCommandHandler> logger)
    {
        _services = services;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceDto> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        Service service = await _services.GetByIdAsync(request.Id, cancellationToken)
                          ?? throw new NotFoundException("Service", request.Id);

        string name = request.Name.Trim();
        if (await _services.NameExistsAsync(name, service.Id, cancellationToken))
        {
            throw new ConflictException($"A service named '{name}' already exists.");
        }

        service.Update(name, request.Description.Trim(), request.DurationInMinutes, request.Price);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Service {ServiceId} updated", service.Id);
        return service.ToDto();
    }
}
