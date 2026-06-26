namespace Z21Sniffer.Core.Model;

public enum TurnoutPosition
{
    Unknown,
    Output1,
    Output2
}

public sealed record TurnoutSnapshot(int Address, TurnoutPosition Position);
