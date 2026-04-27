using Template.Modules.Modules.Auth.Domain;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Template.Infrastructure.Email;

public sealed class UserWelcomeEmailSender : IUserWelcomeEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<UserWelcomeEmailSender> _logger;

    public UserWelcomeEmailSender(IOptions<EmailOptions> options, ILogger<UserWelcomeEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendWelcomeAsync(
        string toEmail,
        string fullName,
        string temporaryPassword,
        CancellationToken cancellationToken)
    {
        var html = EmailTemplates.GetOnboardingEmailBody(temporaryPassword, fullName, loginEmail: toEmail);
        return SendHtmlAsync(
            toEmail,
            "Your Template account",
            html,
            "Sent onboarding welcome email to {Email}.",
            cancellationToken);
    }

    public Task SendPasswordChangedNotificationAsync(string toEmail, string fullName, CancellationToken cancellationToken)
    {
        var html = EmailTemplates.GetPasswordChangedEmailBody(fullName);
        return SendHtmlAsync(
            toEmail,
            "Your Template password was updated",
            html,
            "Sent password-changed notification to {Email}.",
            cancellationToken);
    }

    private async Task SendHtmlAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string successLogMessage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            _logger.LogWarning(
                "EMAIL_IMAP_HOST is not set; skipping SMTP send. Mail for {Email} was not delivered.",
                toEmail);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            _logger.LogWarning(
                "No sender address (EMAIL_USERNAME must be a full email, e.g. noreply@domain.com); cannot send mail to {Recipient}.",
                toEmail);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName ?? "Template", _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        var secure = _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, secure, cancellationToken);

        if (!string.IsNullOrEmpty(_options.SmtpUser))
        {
            await client.AuthenticateAsync(_options.SmtpUser, _options.SmtpPassword ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation(successLogMessage, toEmail);
    }
}
