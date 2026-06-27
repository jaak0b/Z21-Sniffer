using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FakeItEasy;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Presentation.ViewModels;
using Z21Sniffer.UI.Desktop.Views;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public class MainWindowTest
{
    private sealed class StubClock : IClock
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
    }

    private static WorkspaceViewModel BuildWorkspace()
    {
        var settings = A.Fake<ISettingsStore>();
        A.CallTo(() => settings.Load()).Returns(new AppSettings("192.168.0.111", 21105, "en"));
        return WorkspaceFactory.Build(settings, new StubClock());
    }

    private static MainWindow ShownWindow()
    {
        var window = new MainWindow { DataContext = BuildWorkspace() };
        window.Measure(new Avalonia.Size(1180, 720));
        window.Arrange(new Avalonia.Rect(0, 0, 1180, 720));
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static Button Caption(Window window, string name) =>
        window.GetVisualDescendants().OfType<Button>().First(b => b.Name == name);

    private static void Click(Button button)
    {
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaTest]
    public void TitleBar_HasMinimizeMaximizeAndCloseButtons()
    {
        var window = ShownWindow();

        var names = window.GetVisualDescendants().OfType<Button>().Select(b => b.Name).ToList();
        Assert.That(names, Does.Contain("MinimizeButton"));
        Assert.That(names, Does.Contain("MaximizeButton"));
        Assert.That(names, Does.Contain("CloseButton"));
    }

    [AvaloniaTest]
    public void MaximizeButton_TogglesBetweenMaximizedAndNormal()
    {
        var window = ShownWindow();

        Click(Caption(window, "MaximizeButton"));
        Assert.That(window.WindowState, Is.EqualTo(WindowState.Maximized));

        Click(Caption(window, "MaximizeButton"));
        Assert.That(window.WindowState, Is.EqualTo(WindowState.Normal));
    }

    [AvaloniaTest]
    public void MinimizeButton_MinimizesWindow()
    {
        var window = ShownWindow();

        Click(Caption(window, "MinimizeButton"));

        Assert.That(window.WindowState, Is.EqualTo(WindowState.Minimized));
    }

    [AvaloniaTest]
    public void CloseButton_ClosesWindow()
    {
        var window = ShownWindow();
        var closed = false;
        window.Closed += (_, _) => closed = true;

        Click(Caption(window, "CloseButton"));

        Assert.That(closed, Is.True);
    }
}
