using RideWaitTime.Business;

namespace RideWaitTimeMonitor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IQueueTimesClient _queueTimesClient;
    private readonly IWaitTimeThresholdLoader _waitTimeThresholdLoader;
    private readonly INotifier _notifier;
    private readonly Dictionary<string, Ride> _lastWaitTimes = new();

    public Worker(ILogger<Worker> logger, IQueueTimesClient queueTimesClient,
        IWaitTimeThresholdLoader waitTimeThresholdLoader, INotifier notifier)
    {
        _logger = logger;
        _queueTimesClient = queueTimesClient;
        _waitTimeThresholdLoader = waitTimeThresholdLoader;
        _notifier = notifier;
    }

    public async Task MonitorRideWaitTimesAsync(CancellationToken stoppingToken)
    {
        var waitTimeThresholds = _waitTimeThresholdLoader.LoadWaitTimeThresholds();

        var queueTimes = await _queueTimesClient.GetQueueTimesAsync(stoppingToken);

        if (queueTimes is null or { Lands.Length: 0 })
        {
            throw new Exception("No Lands were found for this park");
        }

        var rides = queueTimes.Lands.Where(land => land is { Name: "Coasters" or "Thrill" })
            .SelectMany(land => land.Rides).ToArray();

        if (rides is { Length: 0 })
        {
            throw new Exception("No Coasters or Thrill rides were found for this park");
        }

        var messages = new List<string>();

        foreach (var ride in rides)
        {
            if (waitTimeThresholds.TryGetValue(ride.Name, out var threshold))
            {
                _lastWaitTimes.TryGetValue(ride.Name, out Ride? lastWaitTime);

                if (lastWaitTime is not null)
                {
                    // consecutive runs
                    if (!ride.IsOpen && lastWaitTime.IsOpen)
                    {
                        messages.Add($"{ride.Name} is now closed");
                    }
                    else if (ride.IsOpen && !lastWaitTime.IsOpen && ride.WaitTime <= threshold)
                    {
                        messages.Add($"{ride.Name} is now open with a {ride.WaitTime} minute wait");
                    }
                    else if (ride.WaitTime != lastWaitTime.WaitTime && ride.WaitTime <= threshold)
                    {
                        messages.Add($"{ride.Name} is now a {ride.WaitTime} minute wait");
                    }
                }
                else
                {
                    // first run
                    if (!ride.IsOpen)
                    {
                        messages.Add($"{ride.Name} is now closed");
                    }
                    else if (ride.WaitTime <= threshold)
                    {
                        messages.Add($"{ride.Name} is now a {ride.WaitTime} minute wait");
                    }
                }
            }

            _lastWaitTimes[ride.Name] = ride;
        }
        
        var message = string.Join("\n", messages);
        await _notifier.NotifyAsync(message);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            await MonitorRideWaitTimesAsync(stoppingToken);

            await Task.Delay(60_000 * 5, stoppingToken);
        }
    }
}