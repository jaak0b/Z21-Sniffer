using Autofac.Features.Indexed;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Timeline;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class SourceVisibilityViewModelTest
{
    private sealed class StubRemovalConfirmation : IRemovalConfirmation
    {
        public Task<bool> ConfirmAsync() => Task.FromResult(true);
    }

    private IntervalSourceRegistry _registry = null!;
    private SourceVisibilityViewModel _vm = null!;

    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    [SetUp]
    public void SetUp()
    {
        LocalizationService.Instance.Apply("en");
        _registry = new IntervalSourceRegistry();
        var legend = new FakeIndex<Type, IIntervalLegendDrawingStrategy>(new Dictionary<Type, IIntervalLegendDrawingStrategy>
        {
            [typeof(FeedbackSensorInterval)] = new SensorIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation()),
            [typeof(ConnectionInterval)] = new ConnectionIntervalLegendDrawingStrategy(),
        });
        _vm = new SourceVisibilityViewModel(_registry, legend);
    }

    private FeedbackSensorSource Sensor(int module, int contact) =>
        _registry.GetOrCreate<FeedbackSensorSource>($"sensor:{module}.{contact}", s => s.Sensor = new SensorKey(module, contact));

    [Test]
    public void BuildTree_GroupsSourcesByTypeWithLocalizedLabels()
    {
        Sensor(1, 1);
        _registry.GetOrCreate<ConnectionSource>("connection");

        var tree = _vm.BuildTree();

        Assert.That(tree.Select(group => group.TypeLabel), Is.EqualTo(new[] { "Sensor", "Connection" }));
        Assert.That(tree[0].Sources.Single().Label, Is.EqualTo("M1.1"));
    }

    [Test]
    public void BuildTree_CarriesEachTypesIcon()
    {
        Sensor(1, 1);
        var expected = new SensorIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation()).IconGeometry;

        Assert.That(_vm.BuildTree()[0].IconGeometry, Is.EqualTo(expected));
    }

    [Test]
    public void GroupState_ReflectsItsItemsVisibility()
    {
        Sensor(1, 1);
        var second = Sensor(1, 2);

        Assert.That(_vm.BuildTree()[0].State, Is.EqualTo(SourceVisibilityState.All));

        second.IsVisible = false;
        Assert.That(_vm.BuildTree()[0].State, Is.EqualTo(SourceVisibilityState.Some));

        Sensor(1, 1).IsVisible = false;
        Assert.That(_vm.BuildTree()[0].State, Is.EqualTo(SourceVisibilityState.None));
    }

    [Test]
    public void ItemToggle_FlipsTheSourceVisibility()
    {
        var sensor = Sensor(1, 1);

        _vm.BuildTree()[0].Sources.Single().Toggle();

        Assert.That(sensor.IsVisible, Is.False);
    }

    [Test]
    public void GroupToggle_WhenAllVisible_HidesEverySource()
    {
        var a = Sensor(1, 1);
        var b = Sensor(1, 2);

        _vm.BuildTree()[0].Toggle();

        Assert.That(a.IsVisible, Is.False);
        Assert.That(b.IsVisible, Is.False);
    }

    [Test]
    public void GroupToggle_WhenSomeHidden_ShowsEverySource()
    {
        var a = Sensor(1, 1);
        var b = Sensor(1, 2);
        b.IsVisible = false;

        _vm.BuildTree()[0].Toggle();

        Assert.That(a.IsVisible, Is.True);
        Assert.That(b.IsVisible, Is.True);
    }

    [Test]
    public void HideAll_ThenShowAll_TogglesEverySource()
    {
        var sensor = Sensor(1, 1);
        _registry.GetOrCreate<ConnectionSource>("connection");

        _vm.HideAll();
        Assert.That(_vm.ShownCount, Is.EqualTo(0));

        _vm.ShowAll();
        Assert.That(_vm.ShownCount, Is.EqualTo(2));
        Assert.That(sensor.IsVisible, Is.True);
    }

    [Test]
    public void BuildTree_Filter_KeepsOnlyMatchingRowOrTypeLabels()
    {
        Sensor(1, 1);
        Sensor(2, 2);
        _registry.GetOrCreate<ConnectionSource>("connection");

        var byRow = _vm.BuildTree("M1.1");
        Assert.That(byRow.Select(group => group.TypeLabel), Is.EqualTo(new[] { "Sensor" }));
        Assert.That(byRow[0].Sources.Single().Label, Is.EqualTo("M1.1"));

        var byType = _vm.BuildTree("Connection");
        Assert.That(byType.Select(group => group.TypeLabel), Is.EqualTo(new[] { "Connection" }));
    }

    [Test]
    public void BuildTree_Filter_MatchingOnlyTheTypeLabel_KeepsTheRowsUnderIt()
    {
        Sensor(1, 1);

        var tree = _vm.BuildTree("Sensor");

        Assert.That(tree.Select(group => group.TypeLabel), Is.EqualTo(new[] { "Sensor" }));
        Assert.That(tree[0].Sources.Single().Label, Is.EqualTo("M1.1"));
    }

    [Test]
    public void BuildTree_Filter_MatchingNothing_IsEmpty()
    {
        Sensor(1, 1);
        _registry.GetOrCreate<ConnectionSource>("connection");

        Assert.That(_vm.BuildTree("zzz"), Is.Empty);
    }

    [Test]
    public void Counts_TrackVisibleAndTotalSources()
    {
        Sensor(1, 1);
        var hidden = Sensor(1, 2);
        hidden.IsVisible = false;

        Assert.That(_vm.TotalCount, Is.EqualTo(2));
        Assert.That(_vm.ShownCount, Is.EqualTo(1));
    }
}
