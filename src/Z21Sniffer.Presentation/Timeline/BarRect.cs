namespace Z21Sniffer.Presentation.Timeline;

public readonly record struct BarCorners(bool SquareLeft, bool SquareRight);

public readonly record struct BarRect(double X, double Y, double W, double H, BarCorners Corners = default);

public readonly record struct PlotPoint(double X, double Y);

public readonly record struct BarContentContext(bool ShowContent, TimeSpan FullDuration);

public sealed record ChartViewport(DateTimeOffset Start, DateTimeOffset End, double Width);
