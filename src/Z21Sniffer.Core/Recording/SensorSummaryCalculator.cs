using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class SensorSummaryCalculator
{
    private readonly SensorLabeler _labeler = new();

    public IReadOnlyList<SensorSummary> Summarize(
        IReadOnlyList<SensorInterval> intervals,
        IReadOnlyList<SensorAlias> aliases,
        DateTimeOffset now)
    {
        return intervals
            .GroupBy(interval => interval.Sensor)
            .Select(group =>
            {
                var durations = group
                    .Select(interval => ((interval.End ?? now) - interval.Start).TotalSeconds)
                    .ToList();
                return new SensorSummary(
                    group.Key.Module,
                    group.Key.Contact,
                    _labeler.Label(group.Key, aliases),
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
