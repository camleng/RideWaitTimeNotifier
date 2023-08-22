namespace RideWaitTimeMonitor;

public interface IQueueTimesClient
{
    Task<QueueTimesResponse?> GetQueueTimesAsync(CancellationToken cancellationToken);
}