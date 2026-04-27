using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Common.Utilities;
using Template.Modules.Modules.Users.Domain;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Users.Features;

public sealed class ListUsers : IUsers
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (
                string? search,
                string? status,
                string? page,
                string? pageSize,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var pageNum = 1;
                if (!string.IsNullOrWhiteSpace(page))
                {
                    if (!int.TryParse(page.Trim(), out var parsedPage))
                    {
                        return AppResult<ListUsersResponse>.Failure(
                                Errors.Validation("page", "Page must be a valid integer."))
                            .ToApiResult();
                    }

                    pageNum = parsedPage <= 0 ? 1 : parsedPage;
                }

                var pageSizeNum = 20;
                if (!string.IsNullOrWhiteSpace(pageSize))
                {
                    if (!int.TryParse(pageSize.Trim(), out var parsedSize))
                    {
                        return AppResult<ListUsersResponse>.Failure(
                                Errors.Validation("pageSize", "Page size must be a valid integer."))
                            .ToApiResult();
                    }

                    pageSizeNum = parsedSize is <= 0 or > 100 ? 20 : parsedSize;
                }

                UserStatus? statusFilter = null;
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (!Enum.TryParse<UserStatus>(status.Trim(), ignoreCase: true, out var parsed))
                    {
                        return AppResult<ListUsersResponse>.Failure(
                                Errors.Validation(
                                    "status",
                                    "Invalid status. Allowed values: active, offline, idle, late, inactive."))
                            .ToApiResult();
                    }

                    statusFilter = parsed;
                }

                return (await mediator.Send(
                    new ListUsersQuery(search, statusFilter, pageNum, pageSizeNum),
                    cancellationToken)).ToApiResult();
            })
            .RequireAuthorization();
    }

    public sealed record ListUsersQuery(
        string? Search,
        UserStatus? Status,
        int Page,
        int PageSize) : IAppQuery<ListUsersResponse>;

    public sealed record ListUsersResponse(IReadOnlyCollection<UserListItem> Users, int TotalCount, int Page, int PageSize);

    public sealed record UserRoleDto(Guid Id, string Name, string Code);

    public sealed record UserListItem(
        Guid Id,
        string UserCode,
        string FullName,
        string Email,
        string Phone,
        IReadOnlyList<UserRoleDto> Roles,
        string Status,
        string AvatarInitials);

    public sealed class ListUsersHandler : IRequestHandler<ListUsersQuery, AppResult<ListUsersResponse>>
    {
        private readonly TemplateDbContext _dbContext;

        public ListUsersHandler(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async ValueTask<AppResult<ListUsersResponse>> Handle(ListUsersQuery query, CancellationToken cancellationToken)
        {
            var usersQuery = _dbContext.Users
                .AsNoTracking()
                .Where(u => u.IsActive);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchTerm = query.Search.Trim().ToLowerInvariant();
                usersQuery = usersQuery.Where(u =>
                    u.FullName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm) ||
                    u.Phone.ToLower().Contains(searchTerm) ||
                    u.UserCode.ToLower().Contains(searchTerm));
            }

            if (query.Status is not null)
            {
                usersQuery = usersQuery.Where(u => u.Status == query.Status);
            }

            var totalCount = await usersQuery.CountAsync(cancellationToken);
            var users = await usersQuery
                .OrderBy(u => u.FullName)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var userIds = users.Select(u => u.Id).ToArray();
            var roleRows = await (
                    from ura in _dbContext.UserRoleAssignments.AsNoTracking()
                    join r in _dbContext.Roles.AsNoTracking() on ura.RoleId equals r.Id
                    where userIds.Contains(ura.UserId) && ura.IsActive && r.IsActive
                    select new { ura.UserId, r.Id, r.Name, r.Code })
                .ToListAsync(cancellationToken);

            var rolesByUser = roleRows
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(x => x.Id)
                        .Select(grp =>
                        {
                            var r = grp.First();
                            return new UserRoleDto(r.Id, r.Name, r.Code);
                        })
                        .OrderBy(r => r.Name)
                        .ToArray());

            var primaryRoleLookup = await _dbContext.Roles.AsNoTracking()
                .Where(r => users.Select(u => u.RoleId).Contains(r.Id) && r.IsActive)
                .ToDictionaryAsync(r => r.Id, r => new UserRoleDto(r.Id, r.Name, r.Code), cancellationToken);

            var items = users.Select(u =>
                {
                    IReadOnlyList<UserRoleDto> roles;
                    if (rolesByUser.TryGetValue(u.Id, out var assigned) && assigned.Length > 0)
                    {
                        roles = assigned;
                    }
                    else if (u.RoleId != Guid.Empty && primaryRoleLookup.TryGetValue(u.RoleId, out var primary))
                    {
                        roles = new[] { primary };
                    }
                    else
                    {
                        roles = Array.Empty<UserRoleDto>();
                    }

                    return new UserListItem(
                        u.Id,
                        u.UserCode,
                        u.FullName,
                        u.Email,
                        u.Phone,
                        roles,
                        u.Status.ToApiValue(),
                        u.FullName.ToAvatarInitials());
                })
                .ToArray();

            return AppResult<ListUsersResponse>.Success(new ListUsersResponse(items, totalCount, query.Page, query.PageSize));
        }
    }
}
