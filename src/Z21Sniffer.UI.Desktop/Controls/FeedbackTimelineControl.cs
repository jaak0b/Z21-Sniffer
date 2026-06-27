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
    private const double MinContentWidth = 52;

    private readonly TimelineLayout _layout = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    private readonly IReadOnlyDictionary<string, string> _inkResources = new Dictionary<string, string>
    {
        [TimelineInkKeys.Bar] = "PrimaryBrush",
        [TimelineInkKeys.HighlightedBar] = "WarningBrush",
        [TimelineInkKeys.HighlightOutline] = "DangerBrush",
        [TimelineInkKeys.BarText] = "PrimaryForegroundBrush",
        [TimelineInkKeys.ConnectionText] = "PrimaryForegroundBrush",
        [TimelineInkKeys.Connected] = "SuccessBrush",
        [TimelineInkKeys.Disconnected] = "DangerBrush",
        [TimelineInkKeys.StoppedFlag] = "DangerBrush",
        [TimelineInkKeys.LocoBar] = "SurfaceAltBrush",
        [TimelineInkKeys.LocoSpeedLine] = "PrimaryForegroundBrush",
        [TimelineInkKeys.LocoText] = "TextSecondaryBrush",
        [TimelineInkKeys.TrackPowerProgramming] = "SuccessBrush",
        [TimelineInkKeys.TrackPowerOn] = "PrimaryBrush",
        [TimelineInkKeys.TrackPowerShort] = "DangerBrush",
        [TimelineInkKeys.TrackPowerOff] = "SurfaceAltBrush",
        [TimelineInkKeys.TrackPowerText] = "PrimaryForegroundBrush",
        [TimelineInkKeys.TrackPowerOffText] = "TextPrimaryBrush",
    };

    private List<(Rect Rect, string Text)> _hitAreas = new();
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
        var hit = _hitAreas.FirstOrDefault(area => area.Rect.Contains(absolute));
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
        if (_viewModel is null) return;

        _viewModel.Tick();
        var start = _viewModel.ViewportStart;
        var end = _viewModel.ViewportEnd;

        var gridStep = TimeSpan.FromSeconds(Math.Max(1, (end - start).TotalSeconds / 6));
        var gridPen = new Pen(Brush("BorderBrush", Color.FromArgb(0x33, 0x88, 0x88, 0x88)));
        var axisViewport = new TimelineViewport(start, end, Bounds.Width, Bounds.Height, RowHeight);
        foreach (var tick in _layout.Ticks(axisViewport, gridStep))
        {
            context.DrawLine(gridPen, new Point(tick.X, 0), new Point(tick.X, Bounds.Height));
        }

        var surface = new DrawingContextSurface(context, Brush, _verticalOffset);
        _viewModel.Renderer.Render(
            surface,
            _viewModel.Sources,
            new ChartViewport(start, end, Bounds.Width),
            end,
            _viewModel.HighlightUnderSeconds,
            _verticalOffset,
            Bounds.Height,
            MinContentWidth,
            _viewModel.ZoomFraction);
        _hitAreas = surface.HitAreas;
    }

    private IBrush Brush(string inkKey, Color fallback) =>
        Resolve(_inkResources.TryGetValue(inkKey, out var resource) ? resource : inkKey, fallback);

    private IBrush Resolve(string resourceKey, Color fallback) =>
        this.TryFindResource(resourceKey, ActualThemeVariant, out var value) && value is IBrush brush
            ? brush
            : new SolidColorBrush(fallback);
}
