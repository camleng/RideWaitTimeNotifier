using System.Text.Json.Serialization;

namespace RideWaitTimeMonitor;

public record Ride(
    int Id,
    string Name,
    [property: JsonPropertyName("is_open")]
    bool IsOpen,
    [property: JsonPropertyName("wait_time")]
    int WaitTime,
    [property: JsonPropertyName("last_updated")]
    DateTime LastUpdated
);