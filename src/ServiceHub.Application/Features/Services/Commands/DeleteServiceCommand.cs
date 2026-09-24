using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Domain.Entities;
using ServiceHub.Domain.Interfaces;

namespace ServiceHub.Application.Features.Services.Commands;

public sealed record DeleteServiceCommand(int Id) : IRequest;

public sealed class DeleteServiceCommandValidator : AbstractValidator<DeleteServiceCommand>
{
    public DeleteServiceCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public sealed class DeleteServiceCommandHandler : IRequestHandler<DeleteServiceCommand>
{
    private readonly IServiceRepository _services;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteServiceCommandHandler> _logger;

    public DeleteServiceCommandHandler(IServiceRepository services, IUnitOfWork unitOfWork,
        ILogger<DeleteServiceCommandHandler> logger)
    {
        _services = services;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
    {
        Service service = await _services.GetByIdAsync(request.Id, cancellationToken)
                          ?? throw new NotFoundException("Service", request.Id);

        if (await _services.HasAppointmentsAsync(service.Id, cancellationToken))
        {
            throw new ConflictException(
                "This service has appointments and cannot be deleted. Deactivate it instead.");
        }

        _services.Remove(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Service {ServiceId} deleted", request.Id);
    }
}
