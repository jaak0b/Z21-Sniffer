using System.Globalization;
using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class LocoIntervalSource : AliasedIntervalSource<LocoInterval>
{
    public override bool HighlightsShortIntervals => false;

    public int Address { get; set; }

    protected override string DefaultLabel => Address.ToString(CultureInfo.InvariantCulture);

    public bool HasAlias =>
        Persistence.GetValue<string>($"{Id}/label") is { } stored
        && !string.IsNullOrWhiteSpace(stored)
        && stored != Address.ToString(CultureInfo.InvariantCulture);

    public void Apply(int speed, bool forward, int maxSpeed, DateTimeOffset at)
    {
        if (speed == 0)
        {
            CloseInterval(at, IntervalEndReason.FallingEdge);
            return;
        }

        var interval = CurrentInterval;
        if (interval is null)
        {
            interval = CreateInterval(at);
            interval.MaxSpeed = maxSpeed;
        }

        interval.Samples.Add(new LocoSpeedSample(at, speed, forward));
    }
}
