namespace Z21Sniffer.Presentation.Timeline;

public readonly record struct BarRect(double X, double Y, double W, double H);

public readonly record struct BarContentContext(bool ShowContent, bool Highlighted, TimeSpan FullDuration);

public sealed record ChartViewport(DateTimeOffset Start, DateTimeOffset End, double Width);
