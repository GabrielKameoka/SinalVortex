namespace SinalVortex.Application.Monitoring;

public sealed record QueueSnapshotDto(int High, int Normal, int Low, int Dlq);

public sealed record QueueMonitorEventDto(
    Guid TenantId,
    QueueSnapshotDto Queues,
    string Level,
    string Message,
    DateTime OccurredAtUtc);
