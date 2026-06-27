using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
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
