using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class SensorLegendContentViewModelTest
{
    private sealed class StubRemovalConfirmation : IRemovalConfirmation
    {
        public bool Result { get; set; } = true;

        public Task<bool> ConfirmAsync() => Task.FromResult(Result);
    }

    private IntervalSourceRegistry _registry = null!;
    private FeedbackSensorSource _source = null!;
    private StubRemovalConfirmation _confirmation = null!;
    private SensorLegendContentViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _registry = new IntervalSourceRegistry();
        _source = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1.1");
        _source.Sensor = new SensorKey(1, 1);
        _source.Label = "M1.1";
        _confirmation = new StubRemovalConfirmation();
        _vm = new SensorLegendContentViewModel(_source, _registry, _confirmation);
    }

    [Test]
    public void Label_ReflectsSourceLabel()
    {
        Assert.That(_vm.Label, Is.EqualTo("M1.1"));
    }

    [Test]
    public void CommitRename_WritesEditedLabelBackToSource()
    {
        _vm.Label = "Yard 3";

        _vm.CommitRenameCommand.Execute(null);

        Assert.That(_source.Label, Is.EqualTo("Yard 3"));
    }

    [Test]
    public async Task Remove_WhenConfirmed_RemovesSourceFromRegistry()
    {
        _confirmation.Result = true;

        await _vm.RemoveCommand.ExecuteAsync(null);

        Assert.That(_registry.Sources, Does.Not.Contain(_source));
    }

    [Test]
    public async Task Remove_WhenDeclined_KeepsSource()
    {
        _confirmation.Result = false;

        await _vm.RemoveCommand.ExecuteAsync(null);

        Assert.That(_registry.Sources, Does.Contain(_source));
    }
}
