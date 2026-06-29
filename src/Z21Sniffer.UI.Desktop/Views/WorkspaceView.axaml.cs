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
    private bool _logAtTop = true;

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

    private readonly Dictionary<string, bool> _rowsExpanded = new();

    private void OnRowsMenu(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null || sender is not Button button) return;

        var rows = ViewModel.Rows;
        var localization = LocalizationService.Instance;

        var filter = new TextBox { PlaceholderText = localization["RowsFilter"], Margin = new Thickness(0, 0, 0, 8) };
        var tree = new StackPanel();

        void Rebuild()
        {
            tree.Children.Clear();
            foreach (var group in rows.BuildTree(filter.Text ?? string.Empty)) AddGroup(tree, group, Rebuild);
        }

        filter.TextChanged += (_, _) => Rebuild();

        var showAll = new Button { Content = localization["RowsShowAll"], Padding = new Thickness(10, 4) };
        showAll.Click += (_, _) => { rows.ShowAll(); Rebuild(); };
        var hideAll = new Button { Content = localization["RowsHideAll"], Padding = new Thickness(10, 4) };
        hideAll.Click += (_, _) => { rows.HideAll(); Rebuild(); };

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 8) };
        actions.Children.Add(showAll);
        actions.Children.Add(hideAll);

        var panel = new StackPanel { MinWidth = 280 };
        panel.Children.Add(filter);
        panel.Children.Add(actions);
        panel.Children.Add(new ScrollViewer { Content = tree, MaxHeight = 380 });

        Rebuild();
        new Flyout { Content = panel }.ShowAt(button);
    }

    private IBrush ThemeBrush(string key) => this.FindResource(key) as IBrush ?? Brushes.Transparent;

    private void AddGroup(StackPanel tree, SourceVisibilityGroup group, Action rebuild)
    {
        var expandable = group.Sources.Count > 1;
        var expanded = expandable && _rowsExpanded.TryGetValue(group.TypeLabel, out var stored) && stored;

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("18,*,Auto") };

        if (expandable)
        {
            var chevron = new Path
            {
                Data = Geometry.Parse(expanded ? "M0,1 L8,1 L4,7 Z" : "M1,0 L7,4 L1,8 Z"),
                Fill = ThemeBrush("TextPrimaryBrush"),
                Width = 9,
                Height = 9,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(chevron, 0);
            grid.Children.Add(chevron);
        }

        var icon = new Path
        {
            Data = Geometry.Parse(group.IconGeometry),
            Fill = ThemeBrush("TextSecondaryBrush"),
            Width = 15,
            Height = 15,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        };
        var content = new StackPanel { Orientation = Orientation.Horizontal };
        content.Children.Add(icon);
        content.Children.Add(new TextBlock { Text = group.TypeLabel, FontWeight = FontWeight.Medium, VerticalAlignment = VerticalAlignment.Center });

        var check = new CheckBox
        {
            IsThreeState = true,
            IsChecked = group.State switch
            {
                SourceVisibilityState.All => true,
                SourceVisibilityState.None => false,
                _ => (bool?)null,
            },
            Content = content,
            VerticalAlignment = VerticalAlignment.Center,
        };
        check.Click += (_, _) => { group.Toggle(); rebuild(); };
        Grid.SetColumn(check, 1);
        grid.Children.Add(check);

        var count = new TextBlock
        {
            Text = $"{group.Sources.Count(item => item.IsVisible)}/{group.Sources.Count}",
            FontSize = 12,
            Foreground = ThemeBrush("TextSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 2, 0),
        };
        Grid.SetColumn(count, 2);
        grid.Children.Add(count);

        tree.Children.Add(HoverRow(grid, new Thickness(8, 5), expandable ? () => { _rowsExpanded[group.TypeLabel] = !expanded; rebuild(); } : null));

        if (!expanded) return;
        foreach (var item in group.Sources)
        {
            var captured = item;
            var itemCheck = new CheckBox
            {
                IsChecked = item.IsVisible,
                Content = item.Label,
                Foreground = ThemeBrush(item.IsVisible ? "TextPrimaryBrush" : "TextSecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
            };
            itemCheck.Click += (_, _) => { captured.Toggle(); rebuild(); };
            tree.Children.Add(HoverRow(itemCheck, new Thickness(38, 3, 8, 3), null));
        }
    }

    private Border HoverRow(Control content, Thickness padding, Action? onClick)
    {
        var row = new Border
        {
            Padding = padding,
            Background = Brushes.Transparent,
            Child = content,
            Cursor = new Cursor(StandardCursorType.Hand),
        };
        row.PointerEntered += (_, _) => row.Background = ThemeBrush("ControlHoverBrush");
        row.PointerExited += (_, _) => row.Background = Brushes.Transparent;
        if (onClick is not null) row.PointerPressed += (_, _) => onClick();
        return row;
    }

    private void HookLog()
    {
        if (ViewModel is null || ReferenceEquals(_hookedLog, ViewModel.Log)) return;
        _hookedLog = ViewModel.Log;
        _hookedLog.EntryAppended += OnEntryAppended;
    }

    private void OnEntryAppended(object? sender, EventArgs e)
    {
        if (!_logAtTop) return;

        Dispatcher.UIThread.Post(() =>
        {
            var list = this.FindControl<ListBox>("LogList");
            if (list is { ItemCount: > 0 }) list.ScrollIntoView(0);
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
        scroll.ScrollChanged += (_, _) => _logAtTop = scroll.Offset.Y <= 4;
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
            Background = this.FindResource("SurfaceAltBrush") as IBrush ?? Brushes.Transparent,
            BorderBrush = this.FindResource("PrimaryBrush") as IBrush ?? Brushes.Transparent,
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
