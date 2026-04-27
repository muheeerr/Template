using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Template.Infrastructure;
using Template.Infrastructure.Authorization;
using Template.Infrastructure.Messaging;
using Template.Infrastructure.Persistence;
using Template.Infrastructure.Realtime;
using Template.Modules;
using Template.Modules.Common.Behaviors;
using Template.Modules.Common.Extensions;
using FluentValidation;
using JasperFx.CodeGeneration;
using Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Wolverine;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{

    Log.Information("Starting Template API");

    var builder = WebApplication.CreateBuilder(args);

    Log.Information("Configuring Serilog from application settings");
    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
    });

    Log.Information("Registering infrastructure layer (database, cache, auth services, SignalR)");
    builder.Services.AddTemplateInfrastructure(builder.Configuration);

    Log.Information("Registering application modules");
    builder.Services.AddTemplateModules(builder.Configuration);

    Log.Information("Registering mediator pipeline (validation, transaction, logging behaviors)");
    builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
    builder.Services.AddValidatorsFromAssembly(typeof(TemplateDbContext).Assembly);
    builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
    builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

    Log.Information("Configuring JWT bearer authentication");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["JWT_ISSUER"],
                ValidAudience = builder.Configuration["JWT_AUDIENCE"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT_KEY"] ?? string.Empty)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

    Log.Information("Configuring authorization policies (Authenticated, AdministratorOnly)");
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(Template.Modules.Common.Authorization.AppPolicies.Authenticated, policy => policy.RequireAuthenticatedUser());
        options.AddPolicy(
            Template.Modules.Common.Authorization.AppPolicies.AdministratorOnly,
            policy => policy.RequireRole(Template.Modules.Common.Authorization.AppRoles.Administrator));
    });

    builder.Services.AddHealthChecks().AddDbContextCheck<TemplateDbContext>();

    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "Template API",
                Version = "v1",
                Description = "Template API — authentication, users, email, and SignalR realtime."
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.OrdinalIgnoreCase);
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Name = "Authorization"
            };

            document.Security =
            [
                new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecuritySchemeReference("Bearer", document, null)
                ] = []
            }
            ];

            return Task.CompletedTask;
        });
    });

    Log.Information("Configuring Wolverine message bus");
    builder.Host.UseWolverine(options =>
    {
        options.OptimizeArtifactWorkflow(TypeLoadMode.Auto);
        options.ConfigureTemplateMessaging(builder.Configuration);
        options.Discovery.IncludeAssembly(typeof(TemplateDbContext).Assembly);
        options.Discovery.IncludeAssembly(typeof(InfrastructureServiceExtensions).Assembly);
    });

    var app = builder.Build();

    app.UseMiddleware<Template.Api.Middleware.ApiExceptionMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseMiddleware<ActionClaimsAuthorizationMiddleware>();
    app.UseAuthorization();

    app.MapOpenApi("/openapi/{documentName}.json");
    app.MapScalarApiReference("/scalar", options => options
        .WithTitle("Template API")
        .WithOpenApiRoutePattern("/openapi/{documentName}.json")
        .AddPreferredSecuritySchemes("Bearer")
        .EnablePersistentAuthentication());

    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapHub<TrackingHub>("/hubs/tracking").RequireAuthorization();
    app.MapHub<DashboardHub>("/hubs/dashboard").RequireAuthorization();
    //app.MapTemplateModules();
    app.MapEndpoints();

    Log.Information("Running database seeder");
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
        var credentialHasher = scope.ServiceProvider.GetRequiredService<Template.Modules.Modules.Auth.Domain.ICredentialHasher>();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await ApplicationSeeder.SeedAsync(dbContext, credentialHasher, configuration);

        //var apiActionCatalogSynchronizer = scope.ServiceProvider.GetRequiredService<Template.Modules.Modules.Auth.Domain.IApiActionCatalogSynchronizer>();
        //await apiActionCatalogSynchronizer.SyncAsync(CancellationToken.None);


    }

    Log.Information("Template API is ready");
    app.Run();


}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Template API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
