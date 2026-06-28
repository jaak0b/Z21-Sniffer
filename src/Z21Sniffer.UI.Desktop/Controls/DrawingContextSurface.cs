using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.UI.Desktop.Controls;

public sealed class DrawingContextSurface : ITimelineSurface
{
    private readonly DrawingContext _context;
    private readonly Func<string, Color, IBrush> _resolveBrush;
    private readonly Typeface _typeface = Typeface.Default;
    private readonly double _verticalOffset;
    private Rect _lastBar;

    public DrawingContextSurface(DrawingContext context, Func<string, Color, IBrush> resolveBrush, double verticalOffset)
    {
        _context = context;
        _resolveBrush = resolveBrush;
        _verticalOffset = verticalOffset;
    }

    public void Fill(BarRect rect, TimelineInk ink)
    {
        var shape = Shape(rect);
        if (ink.Key != TimelineInkKeys.StoppedFlag) _lastBar = shape;
        var radius = ink.Key == TimelineInkKeys.StoppedFlag ? 0 : 3;
        _context.DrawRectangle(BrushFor(ink), null, shape, radius, radius);
    }

    public void Stroke(BarRect rect, TimelineInk ink, double thickness) =>
        _context.DrawRectangle(null, new Pen(BrushFor(ink), thickness), Shape(rect), 3, 3);

    public void Text(string text, double x, double y, TimelineInk ink)
    {
        var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, _typeface, 11, BrushFor(ink));
        using (_context.PushClip(_lastBar))
        {
            _context.DrawText(formatted, new Point(x, y - formatted.Height / 2));
        }
    }

    public void Polyline(IReadOnlyList<PlotPoint> points, TimelineInk ink, double thickness)
    {
        if (points.Count < 2) return;

        var pen = new Pen(BrushFor(ink), thickness);
        for (var index = 1; index < points.Count; index++)
        {
            _context.DrawLine(pen,
                new Point(points[index - 1].X, points[index - 1].Y),
                new Point(points[index].X, points[index].Y));
        }
    }

    public void Line(double startX, double startY, double endX, double endY, TimelineInk ink, double thickness, bool dashed)
    {
        var pen = new Pen(BrushFor(ink), thickness)
        {
            DashStyle = dashed ? new DashStyle(new double[] { 2, 2 }, 0) : null,
        };
        _context.DrawLine(pen, new Point(startX, startY), new Point(endX, endY));
    }

    public void Marker(double centerX, double centerY, double radius, TimelineInk ink, double thickness) =>
        _context.DrawEllipse(null, new Pen(BrushFor(ink), thickness), new Point(centerX, centerY), radius, radius);

    private Rect Shape(BarRect rect) => new(rect.X, rect.Y + 3, Math.Max(1, rect.W), Math.Max(1, rect.H - 6));

    private IBrush BrushFor(TimelineInk ink) => _resolveBrush(ink.Key, Colors.Gray);
}
