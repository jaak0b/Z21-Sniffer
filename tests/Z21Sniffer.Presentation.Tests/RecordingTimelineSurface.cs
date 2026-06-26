using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

internal sealed class RecordingTimelineSurface : ITimelineSurface
{
    public sealed record FillOp(BarRect Rect, TimelineInk Ink);

    public sealed record StrokeOp(BarRect Rect, TimelineInk Ink, double Thickness);

    public sealed record TextOp(string Text, double X, double Y, TimelineInk Ink);

    public List<FillOp> Fills { get; } = new();

    public List<StrokeOp> Strokes { get; } = new();

    public List<TextOp> Texts { get; } = new();

    public void Fill(BarRect rect, TimelineInk ink) => Fills.Add(new FillOp(rect, ink));

    public void Stroke(BarRect rect, TimelineInk ink, double thickness) =>
        Strokes.Add(new StrokeOp(rect, ink, thickness));

    public void Text(string text, double x, double y, TimelineInk ink) =>
        Texts.Add(new TextOp(text, x, y, ink));
}
