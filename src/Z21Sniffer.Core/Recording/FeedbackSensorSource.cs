using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class FeedbackSensorSource : IntervalSourceBase<FeedbackSensorInterval>
{
    public SensorKey Sensor { get; set; }

    public string Label
    {
        get => Persistence.GetValue($"{Id}/label", $"M{Sensor.Module}.{Sensor.Contact}") ?? string.Empty;
        set => Persistence.SetValue($"{Id}/label", value);
    }

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
