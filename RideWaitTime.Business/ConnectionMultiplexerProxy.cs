using StackExchange.Redis;

namespace RideWaitTime.Business;

public class ConnectionMultiplexerProxy : IConnectionMultiplexerProxy
{
    private ConnectionMultiplexer? _connectionMultiplexer;

    public ConnectionMultiplexer Connect(string address)
    {
        _connectionMultiplexer = ConnectionMultiplexer.Connect(address);
        return _connectionMultiplexer;
    }
}