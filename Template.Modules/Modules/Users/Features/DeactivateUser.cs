using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Users.Domain;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Users.Features;

public sealed class DeactivateUser : IUsers
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                (await mediator.Send(new DeactivateUserCommand(id), cancellationToken)).ToApiResult("User deactivated successfully"))
            .RequireAuthorization();
    }

    public sealed record DeactivateUserCommand(Guid Id) : IAppCommand;

    public sealed class DeactivateUserHandler : IRequestHandler<DeactivateUserCommand, AppResult>
    {
        private readonly TemplateDbContext _dbContext;

        public DeactivateUserHandler(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async ValueTask<AppResult> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (user is null)
            {
                return AppResult.Failure(Errors.NotFound("User not found."));
            }

            user.IsActive = false;
            user.Status = UserStatus.Inactive;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            return AppResult.Success();
        }
    }
}
