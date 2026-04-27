using Template.Modules.Common.Abstractions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Auth.Domain;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Template.Modules.Common.Behaviors;

public sealed class TransactionBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;

    public TransactionBehavior(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(message, cancellationToken);

        if (message is not IAppCommandBase)
        {
            return response;
        }

        if (response is AppResult result && result.IsSuccess)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var onboardingDispatch = _serviceProvider.GetService<IUserOnboardingEmailDispatch>();
            if (onboardingDispatch is not null)
            {
                await onboardingDispatch.DispatchScheduledAsync(cancellationToken);
            }
        }

        return response;
    }
}
