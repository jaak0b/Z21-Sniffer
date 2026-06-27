namespace Z21Sniffer.Presentation.Timeline;

public interface ITimelineSurface
{
    void Fill(BarRect rect, TimelineInk ink);

    void Stroke(BarRect rect, TimelineInk ink, double thickness);

    void Text(string text, double x, double y, TimelineInk ink);

    void Polyline(IReadOnlyList<PlotPoint> points, TimelineInk ink, double thickness);

    void Hit(BarRect rect, string text);
}
