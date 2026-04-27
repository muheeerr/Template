namespace Template.Infrastructure.Email;

public interface ITransactionalEmailBackgroundQueue
{
    ValueTask EnqueueAsync(TransactionalEmailJob job, CancellationToken cancellationToken = default);
}
