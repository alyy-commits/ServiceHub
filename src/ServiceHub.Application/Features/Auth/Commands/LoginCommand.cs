using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Application.DTOs;

namespace ServiceHub.Application.Features.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(IIdentityService identityService, ILogger<LoginCommandHandler> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Failed attempts are logged inside the IdentityService (they throw UnauthorizedException).
        AuthResponse response = await _identityService.LoginAsync(request.Email.Trim(), request.Password, cancellationToken);
        _logger.LogInformation("User logged in. UserId: {UserId}", response.UserId);
        return response;
    }
}
