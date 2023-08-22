using RideWaitTime.Api;
using RideWaitTimeMonitor;

var builder = WebApplication.CreateBuilder();
builder.Services.AddMemoryCache();
builder.Services.AddTransient<IWaitTimeThresholdLoader, WaitTimeThresholdLoader>();

var app = builder.Build();

app.MapPost("/threshold", (WaitTimeThreshold threshold, IWaitTimeThresholdLoader thresholdLoader) =>
{
    thresholdLoader.SetWaitTimeThreshold(threshold.RideName, threshold.Threshold);
    return threshold;
});

app.MapGet("/threshold", (string rideName, IWaitTimeThresholdLoader thresholdLoader)
    => thresholdLoader.GetWaitTimeThreshold(rideName));

await app.RunAsync();