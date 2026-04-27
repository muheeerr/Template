using Microsoft.Extensions.Configuration;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Template.Infrastructure.Messaging;

public static class WolverineConfigurationExtensions
{
    public static void ConfigureTemplateMessaging(this WolverineOptions options, IConfiguration configuration)
    {
        var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST");
        if (string.IsNullOrWhiteSpace(rabbitHost))
        {
            return;
        }

        options.UseRabbitMq(rabbitMq =>
        {
            rabbitMq.HostName = rabbitHost;
            rabbitMq.UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? throw new InvalidOperationException("Environment variable 'RABBITMQ_USERNAME' is missing when RABBITMQ_HOST is set.");
            rabbitMq.Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? throw new InvalidOperationException("Environment variable 'RABBITMQ_PASSWORD' is missing when RABBITMQ_HOST is set.");
            rabbitMq.VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VIRTUAL_HOST") ?? "/";
        }).AutoProvision();

        options.ListenToRabbitQueue("template.integration.inbound");
        options.PublishAllMessages().ToRabbitExchange("template.events");
    }
}
