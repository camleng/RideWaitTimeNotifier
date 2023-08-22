using System.Net.Http.Json;

namespace RideWaitTimeMonitor;

public class QueueTimesClient : IQueueTimesClient
{
    private readonly HttpClient _client;

    public QueueTimesClient(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("QueueTimes");
    }

    public async Task<QueueTimesResponse?> GetQueueTimesAsync(CancellationToken cancellationToken)
    {
        return await _client.GetFromJsonAsync<QueueTimesResponse>("queue_times.json", cancellationToken);
    }
}