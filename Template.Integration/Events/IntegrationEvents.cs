namespace Template.Integration.Events;

public sealed record SessionRevokedIntegrationEvent(Guid UserId, Guid SessionId, DateTimeOffset RevokedAt);
