namespace Z21Sniffer.Core.Model;

public sealed class SystemCurrentInterval : IntervalBase
{
    public int MaxCurrentMilliamps { get; set; }

    public List<SystemCurrentSample> Samples { get; set; } = new();
}
