using Template.Modules.Common.Abstractions;
using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Auth.Domain;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Auth.Features;

public sealed class GetCurrentUser : IAuth
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/me", async (IMediator mediator, CancellationToken cancellationToken) =>
            (await mediator.Send(new GetCurrentUserQuery(), cancellationToken)).ToApiResult())
            .RequireAuthorization();
    }


    public sealed record GetCurrentUserQuery : IAppQuery<CurrentUserResponse>
    {
        public GetCurrentUserQuery()
        {
        }
    }

    public sealed class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, AppResult<CurrentUserResponse>>
    {
        private readonly TemplateDbContext _dbContext;
        private readonly ICurrentUser _currentUser;

        public GetCurrentUserHandler(TemplateDbContext dbContext, ICurrentUser currentUser)
        {
            _dbContext = dbContext;
            _currentUser = currentUser;
        }

        public async ValueTask<AppResult<CurrentUserResponse>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
        {
            if (_currentUser.UserId is null)
            {
                return AppResult<CurrentUserResponse>.Failure(Errors.Unauthorized());
            }

            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(
                x => x.Id == _currentUser.UserId,
                cancellationToken);
            if (user is null)
            {
                return AppResult<CurrentUserResponse>.Failure(Errors.NotFound("Current user was not found."));
            }

            var roles = await _dbContext.UserRoleAssignments
                .AsNoTracking()
                .Where(x => x.UserId == user.Id && x.IsActive)
                .Join(_dbContext.Roles.AsNoTracking(), era => era.RoleId, role => role.Id, (era, role) => role.Code)
                .ToArrayAsync(cancellationToken);

            var mustChangePassword = await _dbContext.AuthAccounts.AsNoTracking()
                .Where(a => a.UserId == user.Id)
                .Select(a => a.MustChangePassword)
                .FirstOrDefaultAsync(cancellationToken);

            var tenantSlug = await _dbContext.Tenants
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(t => t.Id == user.TenantId && !t.IsDeleted)
                .Select(t => t.Slug)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

            return AppResult<CurrentUserResponse>.Success(
                AuthDtoFactory.ToCurrentUserResponse(user, tenantSlug, roles, _currentUser.Actions, mustChangePassword));
        }
    }
}