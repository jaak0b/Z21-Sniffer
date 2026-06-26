namespace Z21Sniffer.Core.Model;

public sealed record RecordingSession(DateTimeOffset StartedAt, IReadOnlyList<SensorInterval> Intervals);
