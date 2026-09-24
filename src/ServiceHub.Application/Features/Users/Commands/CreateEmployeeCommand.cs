using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ServiceHub.Application.Common.Interfaces;
using ServiceHub.Application.DTOs;

namespace ServiceHub.Application.Features.Users.Commands;

public sealed record CreateEmployeeCommand(string FullName, string Email, string Password) : IRequest<UserDto>;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public sealed class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, UserDto>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<CreateEmployeeCommandHandler> _logger;

    public CreateEmployeeCommandHandler(IIdentityService identityService, ILogger<CreateEmployeeCommandHandler> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<UserDto> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        UserDto user = await _identityService.CreateEmployeeAsync(
            request.FullName.Trim(), request.Email.Trim(), request.Password, cancellationToken);

        _logger.LogInformation("Employee account created. UserId: {UserId}", user.Id);
        return user;
    }
}
