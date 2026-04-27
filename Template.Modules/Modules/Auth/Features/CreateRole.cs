using Template.Modules.Common.Abstractions;
using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Auth.Domain;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Auth.Features;

public sealed class CreateRole : IRoles
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/", async (CreateRoleCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/v1/roles/{result.Value!.Code}", result.Value)
                : result.ToApiResult();
        });
    }
}

public sealed record CreateRoleCommand(string Code, string Name, string? Description) : IAppCommand<CreateRoleResponse>;

public sealed record CreateRoleResponse(Guid Id, string Code, string Name, string? Description);

public sealed class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-z0-9_]+$")
            .WithMessage("Role code must contain only lowercase letters, digits, and underscores.");
        RuleFor(command => command.Name).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Description).MaximumLength(255).When(command => command.Description is not null);
    }
}

public sealed class CreateRoleHandler : IRequestHandler<CreateRoleCommand, AppResult<CreateRoleResponse>>
{
    private readonly TemplateDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    public CreateRoleHandler(TemplateDbContext dbContext, ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    public async ValueTask<AppResult<CreateRoleResponse>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        if (_currentTenant.TenantId is not { } tenantId)
        {
            return AppResult<CreateRoleResponse>.Failure(Errors.Unauthorized());
        }

        var code = command.Code.Trim().ToLowerInvariant();

        var exists = await _dbContext.Roles.AnyAsync(role => role.Code == code, cancellationToken);
        if (exists)
        {
            return AppResult<CreateRoleResponse>.Failure(Errors.Conflict("A role with this code already exists.", "code"));
        }

        var utcNow = DateTimeOffset.UtcNow;
        var role = new AppRole
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = command.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
            IsSystem = false,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        _dbContext.Roles.Add(role);

        return AppResult<CreateRoleResponse>.Success(new CreateRoleResponse(role.Id, role.Code, role.Name, role.Description));
    }
}
