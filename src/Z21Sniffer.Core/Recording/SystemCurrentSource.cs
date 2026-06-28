using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class SystemCurrentSource : IntervalSourceBase<SystemCurrentInterval>
{
    public override bool HighlightsShortIntervals => false;

    public void Apply(int milliamps, int typeCode, string? deviceName, int? maxCurrentMilliamps, DateTimeOffset at)
    {
        if (CurrentInterval is { TypeCode: var openCode } && openCode != typeCode)
            CloseInterval(at, IntervalEndReason.FallingEdge);

        var interval = CurrentInterval;
        if (interval is null)
        {
            interval = CreateInterval(at);
            interval.TypeCode = typeCode;
            interval.DeviceName = deviceName;
            interval.MaxCurrentMilliamps = maxCurrentMilliamps;
        }

        interval.Samples.Add(new SystemCurrentSample(at, milliamps));
    }
}
