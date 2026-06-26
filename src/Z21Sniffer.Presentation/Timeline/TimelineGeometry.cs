using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Presentation.Timeline;

public sealed record TimelineViewport(
    DateTimeOffset Start,
    DateTimeOffset End,
    double Width,
    double Height,
    double RowHeight);

public sealed record TimelineBar(
    SensorKey Sensor,
    int RowIndex,
    double X,
    double Y,
    double Width,
    double Height,
    bool Highlighted,
    double FullDurationSeconds);

public sealed record TimelineTick(DateTimeOffset Time, double X);
