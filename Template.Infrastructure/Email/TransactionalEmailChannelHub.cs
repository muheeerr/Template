using System.Threading.Channels;

namespace Template.Infrastructure.Email;

/// <summary>
/// In-memory queue for post-commit transactional emails; drained by <see cref="TransactionalEmailBackgroundService"/>.
/// </summary>
public sealed class TransactionalEmailChannelHub : ITransactionalEmailBackgroundQueue
{
    private readonly Channel<TransactionalEmailJob> _channel = Channel.CreateUnbounded<TransactionalEmailJob>(new UnboundedChannelOptions
    {
        SingleReader = true,
        AllowSynchronousContinuations = false
    });

    public ValueTask EnqueueAsync(TransactionalEmailJob job, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(job, cancellationToken);

    internal ChannelReader<TransactionalEmailJob> Reader => _channel.Reader;
}
