using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Z21Sniffer.Presentation.Timeline;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.UI.Desktop.Controls;

public sealed class TimeAxisControl : Control
{
    private readonly TimelineLayout _layout = new();
    private readonly ThemeBrushResolver _brushes = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly Typeface _typeface = Typeface.Default;
    private const double CursorLabelPadding = 6;

    private TimelineViewModel? _viewModel;

    public TimeAxisControl()
    {
        ClipToBounds = true;
        _timer.Tick += (_, _) => InvalidateVisual();
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
        if (_viewModel is not null) _viewModel.CursorMoved -= OnCursorMoved;
        _viewModel = DataContext as TimelineViewModel;
        if (_viewModel is not null) _viewModel.CursorMoved += OnCursorMoved;
        InvalidateVisual();
    }

    private void OnCursorMoved(object? sender, EventArgs e) => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_viewModel is null || Bounds.Width <= 0) return;

        var brush = _brushes.Resolve(this, ActualThemeVariant, "TextSecondaryBrush");

        var viewport = new TimelineViewport(_viewModel.ViewportStart, _viewModel.ViewportEnd, Bounds.Width, Bounds.Height, 1);
        var step = TimeSpan.FromSeconds(Math.Max(1, (viewport.End - viewport.Start).TotalSeconds / 6));

        FormattedText? cursorLabel = null;
        var cursorLabelX = 0d;
        if (_viewModel.CursorTime is { } cursorTime && _viewModel.CursorFraction is { } fraction)
        {
            var cursorBrush = _brushes.Resolve(this, ActualThemeVariant, "TextPrimaryBrush");
            cursorLabel = new FormattedText(cursorTime.ToString("HH:mm:ss.ff", CultureInfo.CurrentUICulture),
                CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, _typeface, 10, cursorBrush);
            cursorLabelX = _layout.CenteredLabelX(fraction * Bounds.Width, cursorLabel.Width, Bounds.Width);
        }

        foreach (var tick in _layout.Ticks(viewport, step))
        {
            var text = new FormattedText(tick.Time.ToString("HH:mm:ss", CultureInfo.CurrentUICulture),
                CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, _typeface, 10, brush);
            if (cursorLabel is not null && _layout.RangesOverlap(
                    tick.X + 2 - CursorLabelPadding, tick.X + 2 + text.Width + CursorLabelPadding,
                    cursorLabelX, cursorLabelX + cursorLabel.Width)) continue;

            context.DrawText(text, new Point(tick.X + 2, 2));
        }

        if (cursorLabel is not null) context.DrawText(cursorLabel, new Point(cursorLabelX, 2));
    }
}
