using Microsoft.Extensions.Caching.Memory;

namespace RideWaitTimeMonitor;

public class WaitTimeThresholdLoader : IWaitTimeThresholdLoader
{
    private readonly IMemoryCache _cache;

    public WaitTimeThresholdLoader(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Dictionary<string, int?> LoadWaitTimeThresholds()
    {
        return new Dictionary<string, int?>
        {
            { "Steel Vengeance", 60 },
            { "Millennium Force", 45 },
            { "Maverick", 45 },
            { "Rougarou", 30 },
            { "Valravn", 45 },
            { "GateKeeper", 30 },
            { "Raptor", 30 },
            { "maXair", 15 }
        };
    }

    public int? GetWaitTimeThreshold(string rideName)
    {
        return _cache.Get<int?>(CacheKey(rideName));
    }

    public void SetWaitTimeThreshold(string rideName, int waitTime)
    {
        _cache.Set(CacheKey(rideName), waitTime);
    }

    private static string CacheKey(string rideName)
        => $"Threshold:{rideName}";
}