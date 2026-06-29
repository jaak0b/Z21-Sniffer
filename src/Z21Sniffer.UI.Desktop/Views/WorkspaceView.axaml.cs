using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.ViewModels;
using Z21Sniffer.UI.Desktop.Controls;
using Path = Avalonia.Controls.Shapes.Path;

namespace Z21Sniffer.UI.Desktop.Views;

public partial class WorkspaceView : UserControl
{
    private readonly DispatcherTimer _scrollSync = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private bool _logAtBottom = true;

    private Canvas? _ghostLayer;
    private Border? _ghost;
    private LegendRowViewModel? _dragRow;
    private int _dragFrom = -1;
    private DateTime _lastUserScroll = DateTime.MinValue;
    private ScrollViewer? _hookedLegendScroll;
    private ScrollViewer? _hookedLogScroll;
    private TrafficLogViewModel? _hookedLog;
    private Window? _hookedWindow;

    public WorkspaceView()
    {
        InitializeComponent();
        _scrollSync.Tick += (_, _) => SyncScrollBar();
        AttachedToVisualTree += OnAttached;
        DetachedFromVisualTree += OnDetached;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private WorkspaceViewModel? ViewModel => DataContext as WorkspaceViewModel;

    private Window? HostWindow => TopLevel.GetTopLevel(this) as Window;

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _ghostLayer = this.FindControl<Canvas>("GhostLayer");
        _scrollSync.Start();
        HookLog();
        HookWindowState();
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_hookedWindow is not null) _hookedWindow.PropertyChanged -= OnHostWindowPropertyChanged;
        _hookedWindow = null;
    }

    private void OnTitleBarPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || HostWindow is not { } window) return;
        if (e.ClickCount == 2) ToggleMaximize();
        else window.BeginMoveDrag(e);
    }

    private void OnMinimize(object? sender, RoutedEventArgs e)
    {
        if (HostWindow is { } window) window.WindowState = WindowState.Minimized;
    }

    private void OnMaximizeRestore(object? sender, RoutedEventArgs e) => ToggleMaximize();

    private void OnCloseWindow(object? sender, RoutedEventArgs e) => HostWindow?.Close();

    private void ToggleMaximize()
    {
        if (HostWindow is not { } window) return;
        window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void HookWindowState()
    {
        if (HostWindow is not { } window || ReferenceEquals(_hookedWindow, window)) return;
        _hookedWindow = window;
        window.PropertyChanged += OnHostWindowPropertyChanged;
        UpdateMaximizeGlyph();
    }

    private void OnHostWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty) UpdateMaximizeGlyph();
    }

    private void UpdateMaximizeGlyph()
    {
        var maximized = HostWindow?.WindowState == WindowState.Maximized;
        var maximize = this.FindControl<Path>("MaximizeGlyph");
        var restore = this.FindControl<Path>("RestoreGlyph");
        if (maximize is not null) maximize.IsVisible = !maximized;
        if (restore is not null) restore.IsVisible = maximized;

        var button = this.FindControl<Button>("MaximizeButton");
        if (button is not null) ToolTip.SetTip(button, LocalizationService.Instance[maximized ? "Restore" : "Maximize"]);
    }

    private void OnSessionMenu(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null || sender is not Button button) return;

        var localization = LocalizationService.Instance;
        var save = new Button { Content = localization["SaveSession"], HorizontalAlignment = HorizontalAlignment.Stretch };
        save.Click += (_, _) => ViewModel.SaveSessionCommand.Execute(null);
        var import = new Button
        {
            Content = localization["ImportSession"],
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Command = ViewModel.ImportSessionCommand
        };

        var panel = new StackPanel { Spacing = 4, MinWidth = 150 };
        panel.Children.Add(save);
        panel.Children.Add(import);

        new Flyout { Content = panel }.ShowAt(button);
    }

    private void HookLog()
    {
        if (ViewModel is null || ReferenceEquals(_hookedLog, ViewModel.Log)) return;
        _hookedLog = ViewModel.Log;
        _hookedLog.EntryAppended += OnEntryAppended;
    }

    private void OnEntryAppended(object? sender, EventArgs e)
    {
        if (!_logAtBottom) return;

        Dispatcher.UIThread.Post(() =>
        {
            var list = this.FindControl<ListBox>("LogList");
            if (list is { ItemCount: > 0 }) list.ScrollIntoView(list.ItemCount - 1);
        }, DispatcherPriority.Background);
    }

    private void SyncScrollBar()
    {
        HookLog();
        EnsureLegendHook();
        EnsureLogFollowHook();

        var scrollBar = this.FindControl<ScrollBar>("TimeScrollBar");
        if (scrollBar is null || ViewModel is null) return;

        var timeline = ViewModel.Timeline;
        scrollBar.Maximum = timeline.ScrollMaxSeconds;
        scrollBar.ViewportSize = timeline.WindowSeconds;

        if ((DateTime.UtcNow - _lastUserScroll).TotalMilliseconds < 200) return;
        scrollBar.Value = timeline.ScrollValueSeconds;
    }

    private void EnsureLegendHook()
    {
        var legend = this.FindControl<ListBox>("LegendList");
        var scroll = legend?.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scroll is null || ReferenceEquals(scroll, _hookedLegendScroll)) return;

        _hookedLegendScroll = scroll;
        scroll.ScrollChanged += (_, _) =>
        {
            var bars = this.FindControl<FeedbackTimelineControl>("BarsControl");
            if (bars is not null) bars.VerticalOffset = scroll.Offset.Y;
        };
    }

    private void EnsureLogFollowHook()
    {
        var list = this.FindControl<ListBox>("LogList");
        var scroll = list?.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scroll is null || ReferenceEquals(scroll, _hookedLogScroll)) return;

        _hookedLogScroll = scroll;
        scroll.ScrollChanged += (_, _) =>
            _logAtBottom = scroll.Offset.Y >= scroll.Extent.Height - scroll.Viewport.Height - 4;
    }

    private void OnTimeScroll(object? sender, ScrollEventArgs e)
    {
        _lastUserScroll = DateTime.UtcNow;
        if (sender is ScrollBar bar) ViewModel?.Timeline.SetScrollSeconds(bar.Value);
    }

    private void OnDragHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel is null || sender is not Control { DataContext: LegendRowViewModel row }) return;

        _dragRow = row;
        _dragFrom = ViewModel.Timeline.LegendRows.IndexOf(row);

        _ghost = new Border
        {
            Background = this.FindResource("SurfaceAltBrush") as IBrush ?? Brushes.Gray,
            BorderBrush = this.FindResource("PrimaryBrush") as IBrush ?? Brushes.DodgerBlue,
            BorderThickness = new Thickness(1),
            Opacity = 0.85,
            Padding = new Thickness(8, 4),
            Child = new TextBlock { Text = "≡" }
        };
        _ghostLayer?.Children.Add(_ghost);
        PositionGhost(e.GetPosition(this));

        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragRow is not null) PositionGhost(e.GetPosition(this));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragRow is null || ViewModel is null) return;

        var legend = this.FindControl<ListBox>("LegendList");
        var bars = this.FindControl<FeedbackTimelineControl>("BarsControl");
        if (legend is not null && bars is not null)
        {
            var y = e.GetPosition(legend).Y + bars.VerticalOffset;
            var target = RowIndexAt(y);
            if (target != _dragFrom && _dragFrom >= 0) ViewModel.Timeline.MoveRow(_dragFrom, target);
        }

        if (_ghost is not null) _ghostLayer?.Children.Remove(_ghost);
        _ghost = null;
        _dragRow = null;
        _dragFrom = -1;
        e.Pointer.Capture(null);
    }

    private int RowIndexAt(double y)
    {
        var rows = ViewModel!.Timeline.LegendRows;
        var bottom = 0.0;
        for (var index = 0; index < rows.Count; index++)
        {
            bottom += rows[index].Height;
            if (y < bottom) return index;
        }

        return Math.Max(0, rows.Count - 1);
    }

    private void PositionGhost(Point position)
    {
        if (_ghost is null) return;
        Canvas.SetLeft(_ghost, position.X + 10);
        Canvas.SetTop(_ghost, position.Y - 8);
    }
}
