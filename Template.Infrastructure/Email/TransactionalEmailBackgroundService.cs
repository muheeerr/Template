using Template.Modules.Modules.Auth.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Template.Infrastructure.Email;

public sealed class TransactionalEmailBackgroundService : BackgroundService
{
    private readonly TransactionalEmailChannelHub _hub;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransactionalEmailBackgroundService> _logger;

    public TransactionalEmailBackgroundService(
        TransactionalEmailChannelHub hub,
        IServiceScopeFactory scopeFactory,
        ILogger<TransactionalEmailBackgroundService> logger)
    {
        _hub = hub;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _hub.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IUserWelcomeEmailSender>();
                switch (job)
                {
                    case TransactionalWelcomeEmailJob w:
                        await sender.SendWelcomeAsync(w.ToEmail, w.FullName, w.TemporaryPassword, stoppingToken);
                        break;
                    case TransactionalPasswordChangedEmailJob p:
                        await sender.SendPasswordChangedNotificationAsync(p.ToEmail, p.FullName, stoppingToken);
                        break;
                    default:
                        _logger.LogWarning("Unknown transactional email job type {Type}", job.GetType().Name);
                        break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background transactional email job failed");
            }
        }
    }
}
