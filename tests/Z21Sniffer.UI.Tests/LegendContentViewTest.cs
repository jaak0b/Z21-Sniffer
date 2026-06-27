using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.ViewModels;
using Z21Sniffer.UI.Desktop.Views;

using Path = Avalonia.Controls.Shapes.Path;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public class LegendContentViewTest
{
    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    private static void Render(Control view, object dataContext)
    {
        view.DataContext = dataContext;
        view.Measure(Size.Infinity);
        view.Arrange(new Rect(view.DesiredSize));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaTest]
    public void LocoLegend_RendersIconAndDetailTooltip()
    {
        LocalizationService.Instance.Apply("en");
        var registry = new IntervalSourceRegistry();
        var source = registry.GetOrCreate<LocoIntervalSource>("loco:7", s => s.Address = 7);
        var vm = new LocoLegendContentViewModel(source, registry, new AlwaysConfirm());
        var view = new LocoLegendContentView();

        Render(view, vm);

        Assert.That(view.GetLogicalDescendants().OfType<Path>(), Is.Not.Empty);
        Assert.That(ToolTip.GetTip(view), Is.EqualTo("Locomotive · address 7"));
    }

    [AvaloniaTest]
    public void SensorLegend_RendersIconAndDetailTooltip()
    {
        LocalizationService.Instance.Apply("en");
        var registry = new IntervalSourceRegistry();
        var source = registry.GetOrCreate<FeedbackSensorSource>("sensor:2.3", s => s.Sensor = new SensorKey(2, 3));
        var vm = new SensorLegendContentViewModel(source, registry, new AlwaysConfirm());
        var view = new SensorLegendContentView();

        Render(view, vm);

        Assert.That(view.GetLogicalDescendants().OfType<Path>(), Is.Not.Empty);
        Assert.That(ToolTip.GetTip(view), Is.EqualTo("R-Bus feedback · module 2, contact 3"));
    }

    [AvaloniaTest]
    public void TrackPowerLegend_RendersIconLabelAndDetailTooltip()
    {
        LocalizationService.Instance.Apply("en");
        var vm = new TrackPowerLegendContentViewModel(new TrackPowerSource());
        var view = new TrackPowerLegendContentView();

        Render(view, vm);

        Assert.That(view.GetLogicalDescendants().OfType<Path>(), Is.Not.Empty);
        Assert.That(ToolTip.GetTip(view), Is.EqualTo("Command station track power"));
    }

    [AvaloniaTest]
    public void TrackPowerLegend_IsNotEditableOrDeletable()
    {
        var vm = new TrackPowerLegendContentViewModel(new TrackPowerSource());
        var view = new TrackPowerLegendContentView();

        Render(view, vm);

        Assert.That(view.GetLogicalDescendants().OfType<TextBox>(), Is.Empty);
        Assert.That(view.GetLogicalDescendants().OfType<Button>(), Is.Empty);
    }

    [AvaloniaTest]
    public void ConnectionLegend_RendersIconAndDetailTooltip()
    {
        LocalizationService.Instance.Apply("en");
        var vm = new ConnectionLegendContentViewModel(new ConnectionSource());
        var view = new ConnectionLegendContentView();

        Render(view, vm);

        Assert.That(view.GetLogicalDescendants().OfType<Path>(), Is.Not.Empty);
        Assert.That(ToolTip.GetTip(view), Is.EqualTo("Command station connection"));
    }
}
