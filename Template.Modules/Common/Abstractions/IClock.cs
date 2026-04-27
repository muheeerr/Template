namespace Template.Modules.Common.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
