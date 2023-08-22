using StackExchange.Redis;

namespace RideWaitTime.Business;

public interface IConnectionMultiplexerProxy
{
    ConnectionMultiplexer Connect(string address);
}