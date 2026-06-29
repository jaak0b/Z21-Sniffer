using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Timeline;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class AccessoryStrategyTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private sealed class StubRemovalConfirmation : IRemovalConfirmation
    {
        public Task<bool> ConfirmAsync() => Task.FromResult(true);
    }

    private IntervalSourceRegistry _registry = null!;

    [SetUp]
    public void SetUp()
    {
        LocalizationService.Instance.Apply("en");
        _registry = new IntervalSourceRegistry();
    }

    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    private static AccessorySource WithInterval(TurnoutPosition position)
    {
        var source = new AccessorySource { Id = "accessory:12", Address = 12 };
        source.Apply(position, T0);
        return source;
    }

    [Test]
    public void LegendStrategy_LabelsTheTypeAndTheRow()
    {
        var source = _registry.GetOrCreate<AccessorySource>("accessory:12", s => s.Address = 12);
        var strategy = new AccessoryIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation());

        Assert.That(strategy.TypeLabel, Is.EqualTo("Accessory"));
        Assert.That(strategy.RowLabel(source), Is.EqualTo("A12"));
        Assert.That(strategy.IconGeometry, Does.StartWith("M"));
    }

    [Test]
    public async Task LegendStrategy_WiresRegistryAndConfirmationIntoContent()
    {
        var source = _registry.GetOrCreate<AccessorySource>("accessory:12", s => s.Address = 12);
        var strategy = new AccessoryIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation());

        var content = (AccessoryLegendContentViewModel)strategy.CreateContent(source);
        await content.RemoveCommand.ExecuteAsync(null);

        Assert.That(_registry.Sources, Does.Not.Contain(source));
    }

    [Test]
    public void ChartStrategy_FillsOutput1WithItsOwnInk()
    {
        var strategy = new AccessoryIntervalChartDrawingStrategy();
        var surface = new RecordingTimelineSurface();
        var source = WithInterval(TurnoutPosition.Output1);

        strategy.Draw(source, source.Intervals[0], surface, new BarRect(0, 0, 100, 26),
            new BarContentContext(ShowContent: false, FullDuration: TimeSpan.FromSeconds(4)),
            new ChartViewport(T0, T0.AddSeconds(10), 1000));

        Assert.That(surface.Fills, Has.Some.Matches<RecordingTimelineSurface.FillOp>(f => f.Ink.Key == TimelineInkKeys.AccessoryOutput1));
    }

    [Test]
    public void ChartStrategy_FillsOutput2WithADifferentInk()
    {
        var strategy = new AccessoryIntervalChartDrawingStrategy();
        var surface = new RecordingTimelineSurface();
        var source = WithInterval(TurnoutPosition.Output2);

        strategy.Draw(source, source.Intervals[0], surface, new BarRect(0, 0, 100, 26),
            new BarContentContext(ShowContent: false, FullDuration: TimeSpan.FromSeconds(4)),
            new ChartViewport(T0, T0.AddSeconds(10), 1000));

        Assert.That(surface.Fills, Has.Some.Matches<RecordingTimelineSurface.FillOp>(f => f.Ink.Key == TimelineInkKeys.AccessoryOutput2));
    }

    [Test]
    public void ChartStrategy_WithContent_DrawsLabelWithAddressStateAndDuration()
    {
        var strategy = new AccessoryIntervalChartDrawingStrategy();
        var surface = new RecordingTimelineSurface();
        var source = WithInterval(TurnoutPosition.Output1);

        strategy.Draw(source, source.Intervals[0], surface, new BarRect(10, 4, 100, 26),
            new BarContentContext(ShowContent: true, FullDuration: TimeSpan.FromSeconds(4.2)),
            new ChartViewport(T0, T0.AddSeconds(10), 1000));

        var text = surface.Texts.Single();
        Assert.That(text.Text, Is.EqualTo("A12 · Position 1 · 4.2 s"));
        Assert.That(text.X, Is.EqualTo(15), "text should be inset 5 from the bar's left edge");
        Assert.That(text.Y, Is.EqualTo(17), "text should sit at the bar's vertical centre");
    }

    [Test]
    public void ChartStrategy_WithoutContent_DrawsNoText()
    {
        var strategy = new AccessoryIntervalChartDrawingStrategy();
        var surface = new RecordingTimelineSurface();
        var source = WithInterval(TurnoutPosition.Output1);

        strategy.Draw(source, source.Intervals[0], surface, new BarRect(0, 0, 100, 26),
            new BarContentContext(ShowContent: false, FullDuration: TimeSpan.FromSeconds(4)),
            new ChartViewport(T0, T0.AddSeconds(10), 1000));

        Assert.That(surface.Texts, Is.Empty);
    }

    [Test]
    public void ChartStrategy_RoundsTheDurationToThreeDecimals()
    {
        var strategy = new AccessoryIntervalChartDrawingStrategy();
        var source = WithInterval(TurnoutPosition.Output1);

        var probe = strategy.Probe(source, source.Intervals[0], T0 + TimeSpan.FromTicks(42_367_000));

        Assert.That(probe, Is.EqualTo("A12 · Position 1 · 4.237 s"));
    }

    [Test]
    public void LegendContent_Details_DescribesTheAccessoryAddress()
    {
        var source = _registry.GetOrCreate<AccessorySource>("accessory:12", s => s.Address = 12);
        var content = new AccessoryLegendContentViewModel(source, _registry, new StubRemovalConfirmation());

        Assert.That(content.Details, Is.EqualTo("Accessory · address 12"));
    }

    [Test]
    public void ChartStrategy_WithAlias_UsesTheLabelInsteadOfTheAddress()
    {
        var strategy = new AccessoryIntervalChartDrawingStrategy();
        var surface = new RecordingTimelineSurface();
        var source = WithInterval(TurnoutPosition.Output2);
        source.Label = "Yard ladder";

        strategy.Draw(source, source.Intervals[0], surface, new BarRect(0, 0, 100, 26),
            new BarContentContext(ShowContent: true, FullDuration: TimeSpan.FromSeconds(1)),
            new ChartViewport(T0, T0.AddSeconds(10), 1000));

        Assert.That(surface.Texts, Has.Some.Matches<RecordingTimelineSurface.TextOp>(t =>
            t.Text == "Yard ladder (A12) · Position 2 · 1 s"));
    }

    [Test]
    public void ChartStrategy_Probe_ReportsStateAndElapsed()
    {
        var strategy = new AccessoryIntervalChartDrawingStrategy();
        var source = WithInterval(TurnoutPosition.Output1);

        var probe = strategy.Probe(source, source.Intervals[0], T0.AddSeconds(2));

        Assert.That(probe, Is.EqualTo("A12 · Position 1 · 2 s"));
    }
}
