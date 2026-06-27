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

        if (CurrentInterval is null || CurrentInterval.Forward != forward)
        {
            CloseInterval(at, IntervalEndReason.FallingEdge);
            var opened = CreateInterval(at);
            opened.Forward = forward;
            opened.MaxSpeed = maxSpeed;
        }

        CurrentInterval!.Samples.Add(new LocoSpeedSample(at, speed));
    }
}
