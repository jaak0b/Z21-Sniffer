using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Z21Sniffer.Presentation.Timeline;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.UI.Desktop.Controls;

public sealed class FeedbackTimelineControl : Control
{
    public const double RowHeight = 26;

    private readonly TimelineLayout _layout = new();
    private readonly SensorBarText _barText = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly Typeface _typeface = Typeface.Default;
    private readonly List<(Rect Rect, string Text)> _hitBars = new();

    private TimelineViewModel? _viewModel;
    private double _verticalOffset;
    private bool _dragging;
    private double _lastDragX;

    public FeedbackTimelineControl()
    {
        ClipToBounds = true;
        _timer.Tick += (_, _) => InvalidateVisual();
    }

    public double VerticalOffset
    {
        get => _verticalOffset;
        set
        {
            _verticalOffset = value;
            InvalidateVisual();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _viewModel = DataContext as TimelineViewModel;
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (_viewModel is null || Bounds.Width <= 0) return;
        var anchor = e.GetPosition(this).X / Bounds.Width;
        _viewModel.ZoomByFactor(e.Delta.Y > 0 ? 0.8 : 1.25, Math.Clamp(anchor, 0, 1));
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _dragging = true;
        _lastDragX = e.GetPosition(this).X;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var point = e.GetPosition(this);

        if (_dragging && _viewModel is not null && Bounds.Width > 0)
        {
            var secondsPerPixel = (_viewModel.ViewportEnd - _viewModel.ViewportStart).TotalSeconds / Bounds.Width;
            _viewModel.PanBySeconds(-(point.X - _lastDragX) * secondsPerPixel);
            _lastDragX = point.X;
            return;
        }

        var absolute = new Point(point.X, point.Y + _verticalOffset);
        var hit = _hitBars.FirstOrDefault(b => b.Rect.Contains(absolute));
        ToolTip.SetTip(this, hit.Text);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _dragging = false;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
        _hitBars.Clear();
        if (_viewModel is null) return;

        _viewModel.Tick();
        var rowKeys = _viewModel.Rows.Select(r => r.Sensor).ToList();
        var viewport = new TimelineViewport(_viewModel.ViewportStart, _viewModel.ViewportEnd, Bounds.Width, Bounds.Height, RowHeight);
        var now = _viewModel.ViewportEnd;

        var gridStep = TimeSpan.FromSeconds(Math.Max(1, (viewport.End - viewport.Start).TotalSeconds / 6));
        var gridPen = new Pen(Brush("BorderBrush", Color.FromArgb(0x33, 0x88, 0x88, 0x88)));
        foreach (var tick in _layout.Ticks(viewport, gridStep))
        {
            context.DrawLine(gridPen, new Point(tick.X, 0), new Point(tick.X, Bounds.Height));
        }

        var barBrush = Brush("PrimaryBrush", Color.FromRgb(0x2E, 0x9E, 0x5B));
        var warnBrush = Brush("WarningBrush", Color.FromRgb(0xE0, 0xA4, 0x58));
        var textBrush = Brush("PrimaryForegroundBrush", Colors.White);
        var outlinePen = new Pen(Brush("DangerBrush", Colors.OrangeRed), 2);

        var highlight = _viewModel.HighlightUnderSeconds;
        foreach (var bar in _layout.Bars(viewport, rowKeys, _viewModel.Intervals, now, highlight, _verticalOffset, Bounds.Height))
        {
            var width = Math.Max(1, bar.Width);
            var rect = new Rect(bar.X, bar.Y - _verticalOffset + 3, width, bar.Height - 6);
            context.DrawRectangle(bar.Highlighted ? warnBrush : barBrush, bar.Highlighted ? outlinePen : null, rect, 3, 3);

            var label = _viewModel.Rows.FirstOrDefault(r => r.Sensor == bar.Sensor)?.Label ?? string.Empty;
            var text = _barText.Describe(label, bar.Sensor, TimeSpan.FromSeconds(bar.FullDurationSeconds));
            _hitBars.Add((new Rect(bar.X, bar.Y + 3, width, bar.Height - 6), text));

            DrawInBarLabel(context, rect, text, textBrush);
        }
    }

    private void DrawInBarLabel(DrawingContext context, Rect rect, string text, IBrush brush)
    {
        var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, _typeface, 11, brush);
        if (formatted.Width + 10 > rect.Width) return;
        using (context.PushClip(rect))
        {
            context.DrawText(formatted, new Point(rect.X + 5, rect.Y + (rect.Height - formatted.Height) / 2));
        }
    }

    private IBrush Brush(string key, Color fallback) =>
        this.TryFindResource(key, ActualThemeVariant, out var value) && value is IBrush brush
            ? brush
            : new SolidColorBrush(fallback);
}
