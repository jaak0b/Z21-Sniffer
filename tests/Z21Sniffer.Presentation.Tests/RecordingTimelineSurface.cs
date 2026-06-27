using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

internal sealed class RecordingTimelineSurface : ITimelineSurface
{
    public sealed record FillOp(BarRect Rect, TimelineInk Ink);

    public sealed record StrokeOp(BarRect Rect, TimelineInk Ink, double Thickness);

    public sealed record TextOp(string Text, double X, double Y, TimelineInk Ink);

    public sealed record PolylineOp(IReadOnlyList<PlotPoint> Points, TimelineInk Ink, double Thickness);

    public sealed record MarkerOp(double CenterX, double CenterY, double Radius, TimelineInk Ink, double Thickness);

    public sealed record HitOp(BarRect Rect, string Text);

    public List<FillOp> Fills { get; } = new();

    public List<StrokeOp> Strokes { get; } = new();

    public List<TextOp> Texts { get; } = new();

    public List<PolylineOp> Polylines { get; } = new();

    public List<MarkerOp> Markers { get; } = new();

    public List<HitOp> Hits { get; } = new();

    public void Fill(BarRect rect, TimelineInk ink) => Fills.Add(new FillOp(rect, ink));

    public void Stroke(BarRect rect, TimelineInk ink, double thickness) =>
        Strokes.Add(new StrokeOp(rect, ink, thickness));

    public void Text(string text, double x, double y, TimelineInk ink) =>
        Texts.Add(new TextOp(text, x, y, ink));

    public void Polyline(IReadOnlyList<PlotPoint> points, TimelineInk ink, double thickness) =>
        Polylines.Add(new PolylineOp(points, ink, thickness));

    public void Marker(double centerX, double centerY, double radius, TimelineInk ink, double thickness) =>
        Markers.Add(new MarkerOp(centerX, centerY, radius, ink, thickness));

    public void Hit(BarRect rect, string text) => Hits.Add(new HitOp(rect, text));
}
