using System.Globalization;
using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class SensorBarText
{
    public string Describe(string label, SensorKey sensor, TimeSpan duration)
    {
        var address = $"M{sensor.Module}.{sensor.Contact}";
        var prefix = label == address ? address : $"{label} ({address})";
        var seconds = duration.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
        return $"{prefix} · on {seconds} s";
    }
}
