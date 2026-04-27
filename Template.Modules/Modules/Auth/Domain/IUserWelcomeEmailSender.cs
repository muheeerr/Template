namespace Template.Modules.Modules.Auth.Domain;

public interface IUserWelcomeEmailSender
{
    Task SendWelcomeAsync(string toEmail, string fullName, string temporaryPassword, CancellationToken cancellationToken);

    Task SendPasswordChangedNotificationAsync(string toEmail, string fullName, CancellationToken cancellationToken);
}
