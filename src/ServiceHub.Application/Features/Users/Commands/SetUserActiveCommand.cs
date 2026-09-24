using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Exceptions;
using ServiceHub.Application.Common.Interfaces;

namespace ServiceHub.Application.Features.Users.Commands;

public sealed record SetUserActiveCommand(string UserId, bool IsActive) : IRequest;

public sealed class SetUserActiveCommandValidator : AbstractValidator<SetUserActiveCommand>
{
    public SetUserActiveCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SetUserActiveCommandHandler> _logger;

    public SetUserActiveCommandHandler(IIdentityService identityService, ICurrentUserService currentUser,
        ILogger<SetUserActiveCommandHandler> logger)
    {
        _identityService = identityService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == _currentUser.UserId)
        {
            throw new BadRequestException("You cannot change the active status of your own account.");
        }

        await _identityService.SetUserActiveAsync(request.UserId, request.IsActive, cancellationToken);
        _logger.LogInformation("User {TargetUserId} active status set to {IsActive} by {AdminId}",
            request.UserId, request.IsActive, _currentUser.UserId);
    }
}
