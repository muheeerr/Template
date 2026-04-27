using Template.Modules.Modules.Auth.Domain;
using Microsoft.Extensions.Logging;

namespace Template.Infrastructure.Email;

public sealed class UserOnboardingEmailDispatch : IUserOnboardingEmailDispatch
{
    private readonly ITransactionalEmailBackgroundQueue _queue;
    private readonly ILogger<UserOnboardingEmailDispatch> _logger;
    private WelcomePayload? _welcomePending;
    private PasswordChangedPayload? _passwordChangedPending;

    public UserOnboardingEmailDispatch(
        ITransactionalEmailBackgroundQueue queue,
        ILogger<UserOnboardingEmailDispatch> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public void ScheduleWelcome(string toEmail, string fullName, string temporaryPassword)
    {
        _welcomePending = new WelcomePayload(toEmail, fullName, temporaryPassword);
    }

    public void SchedulePasswordChanged(string toEmail, string fullName)
    {
        _passwordChangedPending = new PasswordChangedPayload(toEmail, fullName);
    }

    public async Task DispatchScheduledAsync(CancellationToken cancellationToken)
    {
        if (_welcomePending is not null)
        {
            var payload = _welcomePending;
            _welcomePending = null;

            try
            {
                await _queue.EnqueueAsync(
                    new TransactionalWelcomeEmailJob(payload.ToEmail, payload.FullName, payload.TemporaryPassword),
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to enqueue onboarding welcome email for {Email}",
                    payload.ToEmail);
            }
        }

        if (_passwordChangedPending is not null)
        {
            var payload = _passwordChangedPending;
            _passwordChangedPending = null;

            try
            {
                await _queue.EnqueueAsync(
                    new TransactionalPasswordChangedEmailJob(payload.ToEmail, payload.FullName),
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to enqueue password-changed email for {Email}",
                    payload.ToEmail);
            }
        }
    }

    private sealed record WelcomePayload(string ToEmail, string FullName, string TemporaryPassword);

    private sealed record PasswordChangedPayload(string ToEmail, string FullName);
}
