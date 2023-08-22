namespace RideWaitTimeMonitor;

public interface IWaitTimeThresholdLoader
{
    Dictionary<string, int?> LoadWaitTimeThresholds();
    int? GetWaitTimeThreshold(string rideName);
    void SetWaitTimeThreshold(string rideName, int waitTime);
}