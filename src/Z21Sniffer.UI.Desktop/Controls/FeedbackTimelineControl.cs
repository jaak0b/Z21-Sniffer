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
    private readonly ThemeBrushResolver _brushes = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    private readonly IReadOnlyDictionary<string, string> _inkResources = new Dictionary<string, string>
    {
        [TimelineInkKeys.Bar] = "SensorBarBrush",
        [TimelineInkKeys.HighlightedBar] = "WarningBrush",
        [TimelineInkKeys.HighlightOutline] = "DangerBrush",
        [TimelineInkKeys.BarText] = "TextPrimaryBrush",
        [TimelineInkKeys.HighlightedBarText] = "PrimaryForegroundBrush",
        [TimelineInkKeys.ConnectionText] = "PrimaryForegroundBrush",
        [TimelineInkKeys.Connected] = "SuccessBrush",
        [TimelineInkKeys.Disconnected] = "DangerBrush",
        [TimelineInkKeys.StoppedFlag] = "DangerBrush",
        [TimelineInkKeys.LocoBar] = "SurfaceAltBrush",
        [TimelineInkKeys.LocoSpeedLine] = "LocoSpeedLineBrush",
        [TimelineInkKeys.LocoBaseline] = "TextSecondaryBrush",
        [TimelineInkKeys.LocoText] = "TextSecondaryBrush",
        [TimelineInkKeys.TrackPowerProgramming] = "SuccessBrush",
        [TimelineInkKeys.TrackPowerOn] = "PrimaryBrush",
        [TimelineInkKeys.TrackPowerShort] = "DangerBrush",
        [TimelineInkKeys.TrackPowerOff] = "SurfaceAltBrush",
        [TimelineInkKeys.TrackPowerText] = "PrimaryForegroundBrush",
        [TimelineInkKeys.TrackPowerOffText] = "TextPrimaryBrush",
        [TimelineInkKeys.SystemCurrentBar] = "SurfaceAltBrush",
        [TimelineInkKeys.SystemCurrentLine] = "WarningBrush",
        [TimelineInkKeys.SystemCurrentBaseline] = "TextSecondaryBrush",
        [TimelineInkKeys.SystemCurrentText] = "TextSecondaryBrush",
    };

    private TimelineViewModel? _viewModel;
    private double _verticalOffset;
    private bool _dragging;
    private double _lastDragX;
    private Point? _lastPointer;

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

        _lastPointer = point;
        UpdateTooltip();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _dragging = false;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _lastPointer = null;
        ToolTip.SetTip(this, null);
    }

    private void UpdateTooltip()
    {
        if (_viewModel is null || _lastPointer is not { } point || Bounds.Width <= 0) return;

        var viewport = new ChartViewport(_viewModel.ViewportStart, _viewModel.ViewportEnd, Bounds.Width);
        var value = _viewModel.Hover.ValueAt(_viewModel.Sources, viewport, _viewModel.ZoomFraction, point.X, point.Y + _verticalOffset);
        ToolTip.SetTip(this, value);
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
        var gridPen = new Pen(Brush("BorderBrush"));
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
        UpdateTooltip();
    }

    private IBrush Brush(string inkKey) =>
        _brushes.Resolve(this, ActualThemeVariant, _inkResources.TryGetValue(inkKey, out var resource) ? resource : inkKey);
}
