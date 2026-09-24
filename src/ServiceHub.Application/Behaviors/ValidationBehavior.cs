using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace ServiceHub.Application.Behaviors;

/// <summary>Runs every FluentValidation validator of a request before its handler executes.</summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly List<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators.ToList();
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Count == 0)
        {
            return await next();
        }

        ValidationContext<TRequest> context = new ValidationContext<TRequest>(request);
        List<ValidationFailure> failures = new List<ValidationFailure>();

        for (int i = 0; i < _validators.Count; i++)
        {
            ValidationResult result = await _validators[i].ValidateAsync(context, cancellationToken);
            for (int j = 0; j < result.Errors.Count; j++)
            {
                failures.Add(result.Errors[j]);
            }
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
