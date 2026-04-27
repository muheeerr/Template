namespace Template.Infrastructure.Email;

public abstract record TransactionalEmailJob;

public sealed record TransactionalWelcomeEmailJob(string ToEmail, string FullName, string TemporaryPassword) : TransactionalEmailJob;

public sealed record TransactionalPasswordChangedEmailJob(string ToEmail, string FullName) : TransactionalEmailJob;
