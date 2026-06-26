namespace Z21Sniffer.Core.Model;

public sealed record SensorInterval(SensorKey Sensor, DateTimeOffset Start, DateTimeOffset? End)
{
    public bool IsOpen => End is null;

    public TimeSpan? Duration => End is { } end ? end - Start : null;
}
