namespace Z21Sniffer.Presentation.Timeline;

public interface ITimelineSurface
{
    void Fill(BarRect rect, TimelineInk ink);

    void Stroke(BarRect rect, TimelineInk ink, double thickness);

    void Text(string text, double x, double y, TimelineInk ink);
}
