namespace RideWaitTime.Business;

public interface INotifier
{
    Task NotifyAsync(string message);
}