namespace Z21Sniffer.Core.Model;

public sealed class AccessoryInterval : IntervalBase
{
    public int Address { get; set; }

    public TurnoutPosition Position { get; set; }
}
