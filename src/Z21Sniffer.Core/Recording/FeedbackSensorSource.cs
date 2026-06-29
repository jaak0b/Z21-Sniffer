using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class FeedbackSensorSource : AliasedIntervalSource<FeedbackSensorInterval>
{
    public SensorKey Sensor { get; set; }

    protected override string DefaultLabel => $"M{Sensor.Module}.{Sensor.Contact}";

    public void Apply(bool occupied, DateTimeOffset at)
    {
        if (occupied && CurrentInterval is null)
        {
            CreateInterval(at).Sensor = Sensor;
        }
        else if (!occupied && CurrentInterval is not null)
        {
            CloseInterval(at, IntervalEndReason.FallingEdge);
        }
    }
}
