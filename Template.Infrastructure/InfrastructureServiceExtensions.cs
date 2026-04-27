using Template.Infrastructure.Authentication;
using Template.Infrastructure.Authorization;
using Template.Infrastructure.Email;
using Template.Infrastructure.Observability;
using Template.Infrastructure.Time;
using Template.Modules;
using Template.Modules.Common.Abstractions;
using Template.Modules.Modules.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Template.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddTemplateInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var defaultConnectionString = Environment.GetEnvironmentVariable("DB_HOST")
            ?? throw new InvalidOperationException("Environment variable 'DB_HOST' is missing.");

        services.AddDbContext<TemplateDbContext>(options =>
        {
            options.UseNpgsql(defaultConnectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure();
                npgsql.MigrationsAssembly(typeof(InfrastructureServiceExtensions).Assembly.GetName().Name);
            });

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") is "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        var redisConnection = Environment.GetEnvironmentVariable("REDIS_HOST") ?? throw new InvalidOperationException("Environment variable 'REDIS_HOST' is missing.");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
            services.AddHybridCache();
        }
        else
        {
            services.AddDistributedMemoryCache();
            services.AddHybridCache();
        }

        services.AddSignalR();
        services.AddHttpContextAccessor();
        services.Configure<JwtOptions>(opts =>
        {
            opts.SigningKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? throw new Exception("Environment variable 'JWT_KEY' is missing.");
            opts.Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "Template";
            opts.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "Template.Clients";
            if (int.TryParse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_MINUTES") ?? "60", out var accessTokenMinutes))
                opts.AccessTokenMinutes = accessTokenMinutes;
            if (int.TryParse(Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_DAYS") ?? "14", out var refreshTokenDays))
                opts.RefreshTokenDays = refreshTokenDays;
        });
        services.Configure<BootstrapOptions>(configuration.GetSection("Bootstrap"));
        services.Configure<EmailOptions>(_ => { });
        services.PostConfigure<EmailOptions>(opts =>
        {
            opts.SmtpHost = Environment.GetEnvironmentVariable("EMAIL_IMAP_HOST") ?? opts.SmtpHost;
            if (int.TryParse(Environment.GetEnvironmentVariable("EMAIL_IMAP_PORT"), out var imapPort))
            {
                opts.SmtpPort = imapPort;
            }

            opts.SmtpUser = Environment.GetEnvironmentVariable("EMAIL_USERNAME") ?? opts.SmtpUser;
            opts.SmtpPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? opts.SmtpPassword;
            opts.OakenServiceUrl = Environment.GetEnvironmentVariable("OAKEN_SERVICE_URL") ?? opts.OakenServiceUrl;

            if (string.IsNullOrWhiteSpace(opts.FromAddress) &&
                opts.SmtpUser?.Contains('@', StringComparison.Ordinal) == true)
            {
                opts.FromAddress = opts.SmtpUser.Trim();
            }
        });

        services.AddSingleton<TransactionalEmailChannelHub>();
        services.AddSingleton<ITransactionalEmailBackgroundQueue>(sp => sp.GetRequiredService<TransactionalEmailChannelHub>());
        services.AddHostedService<TransactionalEmailBackgroundService>();

        services.AddScoped<IUserOnboardingEmailDispatch, UserOnboardingEmailDispatch>();
        services.AddSingleton<IUserWelcomeEmailSender, UserWelcomeEmailSender>();

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<TemplateDbContext>());
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<ICurrentTenant, HttpCurrentTenant>();
        services.AddSingleton<ICredentialHasher, CredentialHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IActionAuthorizationService, ActionAuthorizationService>();
        services.AddScoped<IApiActionCatalogSynchronizer, ApiActionCatalogSynchronizer>();

        return services;
    }
}
