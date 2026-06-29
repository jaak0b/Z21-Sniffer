using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class AccessorySource : AliasedIntervalSource<AccessoryInterval>
{
    public int Address { get; set; }

    protected override string DefaultLabel => $"A{Address}";

    public void Apply(TurnoutPosition position, DateTimeOffset at)
    {
        CloseInterval(at, IntervalEndReason.FallingEdge);

        if (position == TurnoutPosition.Unknown) return;

        var interval = CreateInterval(at);
        interval.Address = Address;
        interval.Position = position;
    }
}
