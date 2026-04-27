using Template.Modules.Common.Abstractions;
using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Auth.Features;

public sealed class Logout : IAuth
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/logout", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new LogoutCommand(), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToApiResult();
        }).RequireAuthorization();
    }
}

public sealed record LogoutCommand : IAppCommand;

public sealed class LogoutHandler : IRequestHandler<LogoutCommand, AppResult>
{
    private readonly TemplateDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public LogoutHandler(TemplateDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<AppResult> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return AppResult.Failure(Errors.Unauthorized());
        }

        var activeSessions = await _dbContext.UserSessions
            .Where(session => session.UserId == _currentUser.UserId && session.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.RevokedAt = DateTimeOffset.UtcNow;
        }

        return AppResult.Success();
    }
}
