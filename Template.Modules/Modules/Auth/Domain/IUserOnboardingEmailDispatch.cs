namespace Template.Modules.Modules.Auth.Domain;

/// <summary>
/// Scoped buffer for transactional emails. After commit, <see cref="DispatchScheduledAsync"/> enqueues jobs for background SMTP delivery.
/// </summary>
public interface IUserOnboardingEmailDispatch
{
    void ScheduleWelcome(string toEmail, string fullName, string temporaryPassword);

    void SchedulePasswordChanged(string toEmail, string fullName);

    /// <summary>Enqueues scheduled messages; does not perform SMTP on the calling thread.</summary>
    Task DispatchScheduledAsync(CancellationToken cancellationToken);
}
