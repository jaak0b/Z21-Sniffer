using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Timeline;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class LegendStrategyTest
{
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

    [Test]
    public void SensorStrategy_CreatesSensorContentForSource()
    {
        var source = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1.1");
        source.Label = "Yard 3";
        var strategy = new SensorIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation());

        var content = strategy.CreateContent(source);

        Assert.That(content, Is.TypeOf<SensorLegendContentViewModel>());
        Assert.That(((SensorLegendContentViewModel)content).Label, Is.EqualTo("Yard 3"));
    }

    [Test]
    public void LocoStrategy_CreatesLocoContentForSource()
    {
        var source = _registry.GetOrCreate<LocoIntervalSource>("loco:482");
        source.Address = 482;
        var strategy = new LocoIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation());

        var content = strategy.CreateContent(source);

        Assert.That(content, Is.TypeOf<LocoLegendContentViewModel>());
        Assert.That(((LocoLegendContentViewModel)content).Label, Is.EqualTo("482"));
    }

    [Test]
    public async Task LocoStrategy_WiresRegistryAndConfirmationIntoContent()
    {
        var source = _registry.GetOrCreate<LocoIntervalSource>("loco:482");
        var strategy = new LocoIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation());

        var content = (LocoLegendContentViewModel)strategy.CreateContent(source);
        await content.RemoveCommand.ExecuteAsync(null);

        Assert.That(_registry.Sources, Does.Not.Contain(source));
    }

    [Test]
    public void ConnectionStrategy_CreatesConnectionContentForSource()
    {
        var source = _registry.GetOrCreate<ConnectionSource>("connection");
        var strategy = new ConnectionIntervalLegendDrawingStrategy();

        var content = strategy.CreateContent(source);

        Assert.That(content, Is.TypeOf<ConnectionLegendContentViewModel>());
        Assert.That(((ConnectionLegendContentViewModel)content).Label, Is.EqualTo(LocalizationService.Instance["Connection"]));
    }

    [Test]
    public async Task SensorStrategy_WiresRegistryAndConfirmationIntoContent()
    {
        var source = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1.1");
        var strategy = new SensorIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation());

        var content = (SensorLegendContentViewModel)strategy.CreateContent(source);
        await content.RemoveCommand.ExecuteAsync(null);

        Assert.That(_registry.Sources, Does.Not.Contain(source));
    }

    [Test]
    public void SensorStrategy_LabelsTheTypeAndTheRow()
    {
        var source = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1.1", s => s.Sensor = new SensorKey(1, 1));
        var strategy = new SensorIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation());

        Assert.That(strategy.TypeLabel, Is.EqualTo("Sensor"));
        Assert.That(strategy.RowLabel(source), Is.EqualTo("M1.1"));
    }

    [Test]
    public void LocoStrategy_LabelsTheTypeAndTheRow()
    {
        var source = _registry.GetOrCreate<LocoIntervalSource>("loco:482", s => s.Address = 482);
        var strategy = new LocoIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation());

        Assert.That(strategy.TypeLabel, Is.EqualTo("Loco"));
        Assert.That(strategy.RowLabel(source), Is.EqualTo("482"));
    }

    [Test]
    public void ConnectionStrategy_LabelsTheTypeAndTheRow()
    {
        var source = _registry.GetOrCreate<ConnectionSource>("connection");
        var strategy = new ConnectionIntervalLegendDrawingStrategy();

        Assert.That(strategy.TypeLabel, Is.EqualTo("Connection"));
        Assert.That(strategy.RowLabel(source), Is.EqualTo("Connection"));
    }

    [Test]
    public void TrackPowerStrategy_LabelsTheTypeAndTheRow()
    {
        var source = _registry.GetOrCreate<TrackPowerSource>("trackpower");
        var strategy = new TrackPowerIntervalLegendDrawingStrategy();

        Assert.That(strategy.TypeLabel, Is.EqualTo("Track power"));
        Assert.That(strategy.RowLabel(source), Is.EqualTo("Track power"));
    }

    [Test]
    public void SystemCurrentStrategy_LabelsTheTypeAndTheRow()
    {
        var source = _registry.GetOrCreate<SystemCurrentSource>("systemcurrent");
        var strategy = new SystemCurrentIntervalLegendDrawingStrategy();

        Assert.That(strategy.TypeLabel, Is.EqualTo("System current"));
        Assert.That(strategy.RowLabel(source), Is.EqualTo("System current"));
    }

    [Test]
    public void EveryStrategy_ProvidesADistinctNonEmptyIconGeometry()
    {
        var icons = new[]
        {
            new SensorIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation()).IconGeometry,
            new LocoIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation()).IconGeometry,
            new ConnectionIntervalLegendDrawingStrategy().IconGeometry,
            new TrackPowerIntervalLegendDrawingStrategy().IconGeometry,
            new SystemCurrentIntervalLegendDrawingStrategy().IconGeometry,
        };

        Assert.That(icons, Has.All.StartWith("M"));
        Assert.That(icons.Distinct().Count(), Is.EqualTo(5));
    }
}
