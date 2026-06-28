namespace Z21Sniffer.Core.Model;

public sealed class SystemCurrentInterval : IntervalBase
{
    public int TypeCode { get; set; }

    public string? DeviceName { get; set; }

    public int? MaxCurrentMilliamps { get; set; }

    public List<SystemCurrentSample> Samples { get; set; } = new();
}
