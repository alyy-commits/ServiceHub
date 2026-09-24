using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Application.DTOs;

namespace ServiceHub.Application.Features.Auth.Commands;

public sealed record RegisterCommand(string FullName, string Email, string Password) : IRequest<AuthResponse>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(IIdentityService identityService, ILogger<RegisterCommandHandler> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        AuthResponse response = await _identityService.RegisterCustomerAsync(
            request.FullName.Trim(), request.Email.Trim(), request.Password, cancellationToken);

        _logger.LogInformation("New customer registered. UserId: {UserId}", response.UserId);
        return response;
    }
}
