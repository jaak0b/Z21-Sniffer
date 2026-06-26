namespace Z21Sniffer.Presentation.Timeline;

public sealed record TimelineViewport(
    DateTimeOffset Start,
    DateTimeOffset End,
    double Width,
    double Height,
    double RowHeight);

public sealed record TimelineTick(DateTimeOffset Time, double X);
