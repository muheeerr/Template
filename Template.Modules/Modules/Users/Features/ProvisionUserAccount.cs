using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Auth.Domain;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Users.Features;

public sealed class ProvisionUserAccount : IUsers
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/{id:guid}/account", async (
                Guid id,
                ProvisionUserAccountCommand command,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            (await mediator.Send(command with { UserId = id }, cancellationToken))
            .ToApiResult("User account provisioned successfully"))
            .RequireAuthorization();
    }
}

public sealed record ProvisionUserAccountCommand(Guid UserId, string? Username, string Password, IReadOnlyCollection<string>? Roles) : IAppCommand;

public sealed class ProvisionUserAccountValidator : AbstractValidator<ProvisionUserAccountCommand>
{
    public ProvisionUserAccountValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8);
    }
}

public sealed class ProvisionUserAccountHandler : IRequestHandler<ProvisionUserAccountCommand, AppResult>
{
    private readonly TemplateDbContext _dbContext;
    private readonly ICredentialHasher _credentialHasher;

    public ProvisionUserAccountHandler(TemplateDbContext dbContext, ICredentialHasher credentialHasher)
    {
        _dbContext = dbContext;
        _credentialHasher = credentialHasher;
    }

    public async ValueTask<AppResult> Handle(ProvisionUserAccountCommand command, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken);
        if (user is null)
        {
            return AppResult.Failure(Errors.NotFound("User not found."));
        }

        var existing = await _dbContext.AuthAccounts.AnyAsync(x => x.UserId == command.UserId, cancellationToken);
        if (existing)
        {
            return AppResult.Failure(Errors.Conflict("An auth account already exists for this user."));
        }

        var username = string.IsNullOrWhiteSpace(command.Username)
            ? user.Email
            : command.Username.Trim().ToLowerInvariant();

        var duplicateUsername = await _dbContext.AuthAccounts.AnyAsync(x => x.Username == username, cancellationToken);
        if (duplicateUsername)
        {
            return AppResult.Failure(Errors.Conflict("Username already exists.", "username"));
        }

        _dbContext.AuthAccounts.Add(new AuthAccount
        {
            Id = Guid.NewGuid(),
            TenantId = user.TenantId,
            UserId = user.Id,
            Username = username,
            PasswordHash = _credentialHasher.Hash(command.Password),
            AuthProvider = AuthProvider.Local,
            MustChangePassword = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        return AppResult.Success();
    }
}
