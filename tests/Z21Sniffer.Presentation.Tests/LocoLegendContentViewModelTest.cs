using NUnit.Framework;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class LocoLegendContentViewModelTest
{
    private sealed class StubRemovalConfirmation : IRemovalConfirmation
    {
        public bool Result { get; set; } = true;

        public Task<bool> ConfirmAsync() => Task.FromResult(Result);
    }

    private IntervalSourceRegistry _registry = null!;
    private LocoIntervalSource _source = null!;
    private StubRemovalConfirmation _confirmation = null!;
    private LocoLegendContentViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _registry = new IntervalSourceRegistry();
        _source = _registry.GetOrCreate<LocoIntervalSource>("loco:482");
        _source.Address = 482;
        _confirmation = new StubRemovalConfirmation();
        _vm = new LocoLegendContentViewModel(_source, _registry, _confirmation);
    }

    [Test]
    public void Label_DefaultsToAddress()
    {
        Assert.That(_vm.Label, Is.EqualTo("482"));
    }

    [Test]
    public void CommitRename_WritesEditedLabelBackToSource()
    {
        _vm.Label = "Express";

        _vm.CommitRenameCommand.Execute(null);

        Assert.That(_source.Label, Is.EqualTo("Express"));
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
