using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FakeItEasy;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.ViewModels;
using Z21Sniffer.UI.Desktop.Controls;
using Z21Sniffer.UI.Desktop.Views;
using Path = Avalonia.Controls.Shapes.Path;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public class WorkspaceViewTest
{
    private sealed class StubClock : IClock
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
    }

    private static WorkspaceContext BuildWorkspace()
    {
        var settings = A.Fake<ISettingsStore>();
        A.CallTo(() => settings.Load()).Returns(new AppSettings("192.168.0.111", 21105, "en"));
        return WorkspaceFactory.BuildContext(settings, new StubClock());
    }

    [AvaloniaTest]
    public void WorkspaceView_RendersEveryLegendRowWithItsConcreteView()
    {
        var context = BuildWorkspace();
        context.Registry.GetOrCreate<FeedbackSensorSource>("sensor:1.1", source => source.Sensor = new SensorKey(1, 1));
        context.Registry.GetOrCreate<ConnectionSource>("connection");
        context.Registry.GetOrCreate<LocoIntervalSource>("loco:3", source => source.Address = 3);
        context.Registry.GetOrCreate<TrackPowerSource>("trackpower");
        context.Registry.GetOrCreate<SystemCurrentSource>("systemcurrent");
        context.Registry.GetOrCreate<AccessorySource>("accessory:12", source => source.Address = 12);

        var window = new Window { Content = new WorkspaceView { DataContext = context.Vm }, Width = 1000, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var rendered = window.GetVisualDescendants().Select(visual => visual.GetType()).ToHashSet();
        Assert.That(rendered, Does.Contain(typeof(SensorLegendContentView)));
        Assert.That(rendered, Does.Contain(typeof(ConnectionLegendContentView)));
        Assert.That(rendered, Does.Contain(typeof(LocoLegendContentView)));
        Assert.That(rendered, Does.Contain(typeof(TrackPowerLegendContentView)));
        Assert.That(rendered, Does.Contain(typeof(SystemCurrentLegendContentView)),
            "the System current legend row fell back to its type name instead of resolving to SystemCurrentLegendContentView");
        Assert.That(rendered, Does.Contain(typeof(AccessoryLegendContentView)),
            "the Accessory legend row fell back to its type name instead of resolving to AccessoryLegendContentView");
    }

    [AvaloniaTest]
    public void TimelineControl_WithAccessoryLane_RendersBothPositionsWithoutThrowing()
    {
        var clock = new StubClock();
        var timeline = WorkspaceFactory.BuildTimelineContext(clock);
        var accessory = timeline.Registry.GetOrCreate<AccessorySource>("accessory:12", source => source.Address = 12);
        accessory.Apply(TurnoutPosition.Output1, clock.Now);
        accessory.Apply(TurnoutPosition.Output2, clock.Now.AddSeconds(3));
        accessory.Apply(TurnoutPosition.Unknown, clock.Now.AddSeconds(6));
        var control = new FeedbackTimelineControl { DataContext = timeline.Vm };
        var window = new Window { Content = control, Width = 800, Height = 300 };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(control.Bounds.Width, Is.GreaterThan(0));
        Assert.That(timeline.Vm.Sources.OfType<AccessorySource>(), Is.Not.Empty);
    }

    [AvaloniaTest]
    public void TimelineControl_PointerMove_SetsCursorTime()
    {
        var clock = new StubClock { Now = DateTimeOffset.UnixEpoch.AddMinutes(1) };
        var timeline = WorkspaceFactory.BuildTimelineContext(clock);
        var control = new FeedbackTimelineControl { DataContext = timeline.Vm };
        var window = new Window { Content = control, Width = 800, Height = 300 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.MouseMove(new Point(400, 150), RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.That(timeline.Vm.CursorTime, Is.Not.Null);
    }

    [AvaloniaTest]
    public void TimelineControl_PointerExited_ClearsCursorTime()
    {
        var clock = new StubClock { Now = DateTimeOffset.UnixEpoch.AddMinutes(1) };
        var timeline = WorkspaceFactory.BuildTimelineContext(clock);
        var control = new FeedbackTimelineControl { DataContext = timeline.Vm };
        var window = new Window { Content = control, Width = 800, Height = 300 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.MouseMove(new Point(400, 150), RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        window.MouseMove(new Point(-20, -20), RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.That(timeline.Vm.CursorTime, Is.Null);
    }

    [AvaloniaTest]
    public void WorkspaceView_RendersLegendIconsStrokedForSystemCurrentAndFilledOtherwise()
    {
        var context = BuildWorkspace();
        context.Registry.GetOrCreate<SystemCurrentSource>("systemcurrent");
        context.Registry.GetOrCreate<ConnectionSource>("connection");

        var window = new Window { Content = new WorkspaceView { DataContext = context.Vm }, Width = 1000, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var legend = window.GetVisualDescendants().OfType<ListBox>().First(list => list.Name == "LegendList");
        var icons = legend.GetVisualDescendants().OfType<Path>().Where(path => path.IsVisible).ToList();

        Assert.That(icons.Any(path => path.Classes.Contains("legendIconStroked")), Is.True, "the System current icon should render stroked");
        Assert.That(icons.Any(path => path.Classes.Contains("legendIcon")), Is.True, "other icons should render filled");
    }

    [AvaloniaTest]
    public void WorkspaceView_HidingASourceViaRows_RemovesItsLegendRow()
    {
        var context = BuildWorkspace();
        context.Registry.GetOrCreate<FeedbackSensorSource>("sensor:1.1", source => source.Sensor = new SensorKey(1, 1));
        context.Registry.GetOrCreate<ConnectionSource>("connection");

        var window = new Window { Content = new WorkspaceView { DataContext = context.Vm }, Width = 1000, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var sensorItem = context.Vm.Rows.BuildTree().SelectMany(group => group.Sources).First(item => item.Source.Id == "sensor:1.1");
        sensorItem.Toggle();
        Dispatcher.UIThread.RunJobs();

        var rendered = window.GetVisualDescendants().Select(visual => visual.GetType()).ToHashSet();
        Assert.That(rendered, Does.Not.Contain(typeof(SensorLegendContentView)));
        Assert.That(rendered, Does.Contain(typeof(ConnectionLegendContentView)));
    }

    [AvaloniaTest]
    public void WorkspaceView_RendersTheSystemCurrentLegendWithItsOwnView()
    {
        var view = new WorkspaceView();
        var content = new SystemCurrentLegendContentViewModel(new Z21Sniffer.Core.Recording.SystemCurrentSource { Id = "systemcurrent" });

        var template = view.DataTemplates.FirstOrDefault(t => t.Match(content));

        Assert.That(template, Is.Not.Null, "WorkspaceView has no DataTemplate for SystemCurrentLegendContentViewModel, so the legend falls back to the type name");
        Assert.That(template!.Build(content), Is.TypeOf<SystemCurrentLegendContentView>());
    }

    [AvaloniaTest]
    public void WorkspaceView_LoadsAndHostsTimelineControl()
    {
        var window = new Window
        {
            Content = new WorkspaceView { DataContext = BuildWorkspace().Vm },
            Width = 1000,
            Height = 600
        };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var control = window.GetVisualDescendants().OfType<FeedbackTimelineControl>().FirstOrDefault();
        Assert.That(control, Is.Not.Null);
    }

    [AvaloniaTest]
    public void Toolbar_Separators_AreVerticallyCenteredWithFixedHeight()
    {
        var window = new Window
        {
            Content = new WorkspaceView { DataContext = BuildWorkspace().Vm },
            Width = 1000,
            Height = 600
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var separators = window.GetVisualDescendants().OfType<Border>().Where(b => b.Width == 1).ToList();
        Assert.That(separators, Has.Count.EqualTo(2));
        Assert.That(separators, Has.All.Matches<Border>(
            b => b.VerticalAlignment == Avalonia.Layout.VerticalAlignment.Center && b.Height > 0));
    }

    [AvaloniaTest]
    public void WorkspaceView_LogTab_RendersAppendedEntries()
    {
        var workspace = BuildWorkspace();
        var window = new Window
        {
            Content = new WorkspaceView { DataContext = workspace.Vm },
            Width = 1000,
            Height = 600
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        workspace.Vm.Log.StartRecording();
        workspace.Ingest.Apply([new SensorState(new SensorKey(1, 1), Occupied: true)], DateTimeOffset.UnixEpoch);
        var tabs = window.GetVisualDescendants().OfType<TabControl>().First();
        tabs.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();

        var logList = window.GetVisualDescendants().OfType<ListBox>().FirstOrDefault(l => l.Name == "LogList");
        Assert.That(logList, Is.Not.Null);
        Assert.That(workspace.Vm.Log.Filtered, Is.Not.Empty);
    }

    [AvaloniaTest]
    public void TimelineControl_WithFeedback_RendersWithoutThrowing()
    {
        var clock = new StubClock();
        var timeline = WorkspaceFactory.BuildTimelineContext(clock);
        timeline.Ingest.Apply([new SensorState(new SensorKey(1, 1), Occupied: true)], clock.Now);
        var control = new FeedbackTimelineControl { DataContext = timeline.Vm };
        var window = new Window { Content = control, Width = 800, Height = 300 };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(control.Bounds.Width, Is.GreaterThan(0));
    }

    [AvaloniaTest]
    public void TimelineControl_WithConnectionLaneAndStoppedIntervals_RendersWithoutThrowing()
    {
        var clock = new StubClock();
        var timeline = WorkspaceFactory.BuildTimelineContext(clock);
        timeline.Ingest.Apply([new SensorState(new SensorKey(1, 1), Occupied: true)], clock.Now);
        var connection = timeline.Registry.GetOrCreate<ConnectionSource>("connection");
        connection.Set(connected: true, clock.Now);
        var stoppedAt = clock.Now.AddSeconds(5);
        foreach (var source in timeline.Registry.Sources)
            source.CloseOpenIntervals(stoppedAt, IntervalEndReason.Stopped);
        var control = new FeedbackTimelineControl { DataContext = timeline.Vm };
        var window = new Window { Content = control, Width = 800, Height = 300 };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(control.Bounds.Width, Is.GreaterThan(0));
        Assert.That(timeline.Vm.Sources.OfType<ConnectionSource>(), Is.Not.Empty);
    }

    [AvaloniaTest]
    public void TimelineControl_WithLocoLane_RendersSpeedLineWithoutThrowing()
    {
        var clock = new StubClock();
        var timeline = WorkspaceFactory.BuildTimelineContext(clock);
        var loco = timeline.Registry.GetOrCreate<LocoIntervalSource>("loco:3", source => source.Address = 3);
        loco.Apply(40, forward: true, maxSpeed: 126, clock.Now);
        loco.Apply(90, forward: true, maxSpeed: 126, clock.Now.AddSeconds(2));
        var control = new FeedbackTimelineControl { DataContext = timeline.Vm };
        var window = new Window { Content = control, Width = 800, Height = 300 };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(control.Bounds.Width, Is.GreaterThan(0));
        Assert.That(timeline.Vm.Sources.OfType<LocoIntervalSource>(), Is.Not.Empty);
    }

    [AvaloniaTest]
    public void TimelineControl_WithSystemCurrentLane_RendersAndTooltipsTheReadingWithoutThrowing()
    {
        var clock = new StubClock();
        var timeline = WorkspaceFactory.BuildTimelineContext(clock);
        var current = timeline.Registry.GetOrCreate<SystemCurrentSource>("systemcurrent");
        current.Apply(800, typeCode: 513, deviceName: "Z21 (black)", maxCurrentMilliamps: 3000, clock.Now);
        current.Apply(1600, typeCode: 513, deviceName: "Z21 (black)", maxCurrentMilliamps: 3000, clock.Now.AddSeconds(2));
        var control = new FeedbackTimelineControl { DataContext = timeline.Vm };
        var window = new Window { Content = control, Width = 800, Height = 300 };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(control.Bounds.Width, Is.GreaterThan(0));
        Assert.That(timeline.Vm.Sources.OfType<SystemCurrentSource>(), Is.Not.Empty);
    }

    [AvaloniaTest]
    public void TimelineControl_WithTrackPowerLane_RendersAllStatesWithoutThrowing()
    {
        var clock = new StubClock();
        var timeline = WorkspaceFactory.BuildTimelineContext(clock);
        var power = timeline.Registry.GetOrCreate<TrackPowerSource>("trackpower");
        power.Set(TrackPowerStatus.On, clock.Now);
        power.Set(TrackPowerStatus.Off, clock.Now.AddSeconds(2));
        power.Set(TrackPowerStatus.Short, clock.Now.AddSeconds(4));
        power.Set(TrackPowerStatus.Programming, clock.Now.AddSeconds(6));
        var control = new FeedbackTimelineControl { DataContext = timeline.Vm };
        var window = new Window { Content = control, Width = 800, Height = 300 };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(control.Bounds.Width, Is.GreaterThan(0));
        Assert.That(timeline.Vm.Sources.OfType<TrackPowerSource>(), Is.Not.Empty);
    }

    [AvaloniaTest]
    public void TimelineControl_WithLocoLaneStartingBeforeViewport_RendersWithoutThrowing()
    {
        var clock = new StubClock();
        var timeline = WorkspaceFactory.BuildTimelineContext(clock);
        var loco = timeline.Registry.GetOrCreate<LocoIntervalSource>("loco:3", source => source.Address = 3);
        loco.Apply(83, forward: true, maxSpeed: 126, clock.Now);
        loco.Apply(10, forward: true, maxSpeed: 126, clock.Now.AddSeconds(1));
        loco.Apply(91, forward: true, maxSpeed: 126, clock.Now.AddSeconds(2));
        loco.Apply(34, forward: true, maxSpeed: 126, clock.Now.AddSeconds(3));
        clock.Now = DateTimeOffset.UnixEpoch.AddSeconds(200);
        timeline.Vm.Tick();
        var control = new FeedbackTimelineControl { DataContext = timeline.Vm };
        var window = new Window { Content = control, Width = 800, Height = 300 };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(control.Bounds.Width, Is.GreaterThan(0));
        Assert.That(timeline.Vm.ViewportStart, Is.GreaterThan(loco.Intervals[0].Start));
    }

    [AvaloniaTest]
    public void TimelineControl_EmptyArea_IsHitTestVisibleForPanAndZoom()
    {
        var clock = new StubClock();
        var vm = WorkspaceFactory.BuildTimeline(clock);
        var control = new FeedbackTimelineControl { DataContext = vm };
        var window = new Window { Content = control, Width = 800, Height = 300 };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var hit = window.GetVisualAt(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2));
        Assert.That(hit, Is.SameAs(control));
    }
}
