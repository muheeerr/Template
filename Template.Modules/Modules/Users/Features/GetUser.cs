using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Users.Features;

public sealed class GetUser : IUsers
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                (await mediator.Send(new GetUserQuery(id), cancellationToken)).ToApiResult())
            .RequireAuthorization();
    }

    public sealed record GetUserQuery(Guid Id) : IAppQuery<GetUserResponse>;

    public sealed record UserRoleDto(Guid Id, string Name, string Code);

    public sealed record GetUserResponse(
        Guid Id,
        string UserCode,
        string FullName,
        string Email,
        string Phone,
        IReadOnlyList<UserRoleDto> Roles,
        string Status,
        string AvatarInitials,
        TimeOnly WorkingHoursStart,
        TimeOnly WorkingHoursEnd);

    public sealed class GetUserHandler : IRequestHandler<GetUserQuery, AppResult<GetUserResponse>>
    {
        private readonly TemplateDbContext _dbContext;

        public GetUserHandler(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async ValueTask<AppResult<GetUserResponse>> Handle(GetUserQuery query, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);
            if (user is null)
            {
                return AppResult<GetUserResponse>.Failure(Errors.NotFound("User not found."));
            }

            var roleRows = await (
                    from ura in _dbContext.UserRoleAssignments.AsNoTracking()
                    join r in _dbContext.Roles.AsNoTracking() on ura.RoleId equals r.Id
                    where ura.UserId == user.Id && ura.IsActive && r.IsActive
                    select new { r.Id, r.Name, r.Code })
                .ToListAsync(cancellationToken);

            var roles = roleRows
                .GroupBy(x => x.Id)
                .Select(g =>
                {
                    var r = g.First();
                    return new UserRoleDto(r.Id, r.Name, r.Code);
                })
                .OrderBy(r => r.Name)
                .ToList();

            if (roles.Count == 0 && user.RoleId != Guid.Empty)
            {
                var primary = await _dbContext.Roles.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == user.RoleId && r.IsActive, cancellationToken);
                if (primary is not null)
                {
                    roles.Add(new UserRoleDto(primary.Id, primary.Name, primary.Code));
                }
            }

            return AppResult<GetUserResponse>.Success(
                new GetUserResponse(
                    user.Id,
                    user.UserCode,
                    user.FullName,
                    user.Email,
                    user.Phone,
                    roles,
                    user.Status.ToApiValue(),
                    user.FullName.ToAvatarInitials(),
                    user.WorkingHoursStart,
                    user.WorkingHoursEnd));
        }
    }
}
