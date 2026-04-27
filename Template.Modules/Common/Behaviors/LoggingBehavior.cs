using Mediator;
using Microsoft.Extensions.Logging;

namespace Template.Modules.Common.Behaviors;

public sealed class LoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    private readonly ILogger<LoggingBehavior<TMessage, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var messageName = typeof(TMessage).Name;
        var startedAt = DateTime.UtcNow;

        _logger.LogInformation("Handling {MessageType}", messageName);

        try
        {
            var response = await next(message, cancellationToken);
            _logger.LogInformation(
                "Handled {MessageType} in {ElapsedMilliseconds}ms",
                messageName,
                (DateTime.UtcNow - startedAt).TotalMilliseconds);
            return response;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error handling {MessageType} after {ElapsedMilliseconds}ms",
                messageName,
                (DateTime.UtcNow - startedAt).TotalMilliseconds);
            throw;
        }
    }
}
