using FluentValidation;
using Mediator;

namespace Template.Modules.Common.Behaviors;

public sealed class ValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    private readonly IEnumerable<IValidator<TMessage>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TMessage>> validators)
    {
        _validators = validators;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(message, cancellationToken);
        }

        var context = new ValidationContext<TMessage>(message);
        var failures = (await Task.WhenAll(
                _validators.Select(validator => validator.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToArray();

        if (failures.Length > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(message, cancellationToken);
    }
}
