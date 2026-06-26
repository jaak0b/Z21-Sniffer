using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class SensorSummaryCalculator
{
    public IReadOnlyList<SensorSummary> Summarize(IReadOnlyList<FeedbackSensorSource> sources, DateTimeOffset now)
    {
        return sources
            .Where(source => source.Intervals.Count > 0)
            .Select(source =>
            {
                var durations = source.Intervals
                    .Select(interval => ((interval.End ?? now) - interval.Start).TotalSeconds)
                    .ToList();
                return new SensorSummary(
                    source.Sensor.Module,
                    source.Sensor.Contact,
                    source.Label,
                    durations.Count,
                    durations.Sum(),
                    durations.Min(),
                    durations.Max());
            })
            .OrderBy(summary => summary.Module)
            .ThenBy(summary => summary.Contact)
            .ToList();
    }
}
