namespace Z21Sniffer.Core.Model;

public sealed class LocoInterval : IntervalBase
{
    public int MaxSpeed { get; set; }

    public List<LocoSpeedSample> Samples { get; set; } = new();
}
