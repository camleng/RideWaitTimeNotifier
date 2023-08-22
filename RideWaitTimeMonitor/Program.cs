using RideWaitTime.Business;
using RideWaitTimeMonitor;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        var queueTimesBaseAddress = ctx.Configuration["QueueTimes:BaseAddress"];
        if (string.IsNullOrWhiteSpace(queueTimesBaseAddress))
        {
            throw new Exception("QueueTimes:BaseAddress is empty");
        }
        services.AddHttpClient("QueueTimes",
            client =>
            {
                client.BaseAddress = new Uri(queueTimesBaseAddress);
            });
        services.AddTransient<INotifier, Notifier>();
        services.AddTransient<IQueueTimesClient, QueueTimesClient>();
        services.AddTransient<IWaitTimeThresholdLoader, WaitTimeThresholdLoader>();
        services.AddMemoryCache();
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();