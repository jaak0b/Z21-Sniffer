using NUnit.Framework;
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
}
