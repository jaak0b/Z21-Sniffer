using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class SystemCurrentSource : IntervalSourceBase<SystemCurrentInterval>
{
    public void Apply(int milliamps, int maxCurrentMilliamps, DateTimeOffset at)
    {
        var interval = CurrentInterval ?? CreateInterval(at);
        interval.MaxCurrentMilliamps = maxCurrentMilliamps;
        interval.Samples.Add(new SystemCurrentSample(at, milliamps));
    }
}
