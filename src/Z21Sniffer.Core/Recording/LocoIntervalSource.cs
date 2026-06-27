using System.Globalization;
using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class LocoIntervalSource : IntervalSourceBase<LocoInterval>
{
    public int Address { get; set; }

    public string Label
    {
        get => Persistence.GetValue($"{Id}/label", Address.ToString(CultureInfo.InvariantCulture)) ?? string.Empty;
        set => Persistence.SetValue($"{Id}/label", value);
    }

    public void Apply(int speed, bool forward, int maxSpeed, DateTimeOffset at)
    {
        if (speed == 0)
        {
            CloseInterval(at, IntervalEndReason.FallingEdge);
            return;
        }

        var interval = CurrentInterval;
        if (interval is null || interval.Forward != forward)
        {
            CloseInterval(at, IntervalEndReason.FallingEdge);
            interval = CreateInterval(at);
            interval.Forward = forward;
            interval.MaxSpeed = maxSpeed;
        }

        interval.Samples.Add(new LocoSpeedSample(at, speed));
    }
}
