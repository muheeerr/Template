using Template.Modules.Common.Abstractions;
using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Auth.Domain;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Auth.Features;

public sealed class ChangePassword : IAuth
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/change-password", async (ChangePasswordCommand command, IMediator mediator, CancellationToken cancellationToken) =>
            (await mediator.Send(command, cancellationToken)).ToApiResult("Password updated successfully"))
            .RequireAuthorization();
    }

    public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword, string ConfirmNewPassword) : IAppCommand;

    public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordValidator()
        {
            RuleFor(c => c.CurrentPassword).NotEmpty();
            RuleFor(c => c.NewPassword).NotEmpty().MinimumLength(8);
            RuleFor(c => c.ConfirmNewPassword).Equal(c => c.NewPassword).WithMessage("Must match the new password.");
        }
    }

    public sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, AppResult>
    {
        private readonly TemplateDbContext _dbContext;
        private readonly ICurrentUser _currentUser;
        private readonly ICredentialHasher _credentialHasher;
        private readonly IUserOnboardingEmailDispatch _emailDispatch;

        public ChangePasswordHandler(
            TemplateDbContext dbContext,
            ICurrentUser currentUser,
            ICredentialHasher credentialHasher,
            IUserOnboardingEmailDispatch emailDispatch)
        {
            _dbContext = dbContext;
            _currentUser = currentUser;
            _credentialHasher = credentialHasher;
            _emailDispatch = emailDispatch;
        }

        public async ValueTask<AppResult> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
        {
            if (_currentUser.UserId is null)
            {
                return AppResult.Failure(Errors.Unauthorized());
            }

            var account = await _dbContext.AuthAccounts.FirstOrDefaultAsync(
                x => x.UserId == _currentUser.UserId,
                cancellationToken);
            if (account is null || string.IsNullOrWhiteSpace(account.PasswordHash))
            {
                return AppResult.Failure(Errors.Validation("password", "Password change is not available for this account."));
            }

            if (!_credentialHasher.Verify(account.PasswordHash, command.CurrentPassword))
            {
                return AppResult.Failure(Errors.Validation("currentPassword", "Current password is incorrect."));
            }

            if (_credentialHasher.Verify(account.PasswordHash, command.NewPassword))
            {
                return AppResult.Failure(Errors.Validation("newPassword", "Choose a password different from your current password."));
            }

            account.PasswordHash = _credentialHasher.Hash(command.NewPassword);
            account.MustChangePassword = false;
            account.LastPasswordChangedAt = DateTimeOffset.UtcNow;
            account.TokenVersion++;
            account.UpdatedAt = DateTimeOffset.UtcNow;

            var sessions = await _dbContext.UserSessions
                .Where(s => s.UserId == account.UserId && s.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var session in sessions)
            {
                session.RevokedAt = DateTimeOffset.UtcNow;
            }

            var user = await _dbContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == account.UserId, cancellationToken);
            if (user is not null && !string.IsNullOrWhiteSpace(user.Email))
            {
                _emailDispatch.SchedulePasswordChanged(user.Email.Trim().ToLowerInvariant(), user.FullName);
            }

            return AppResult.Success();
        }
    }
}
