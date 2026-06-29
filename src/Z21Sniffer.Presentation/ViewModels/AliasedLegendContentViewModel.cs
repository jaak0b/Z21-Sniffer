using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Presentation.ViewModels;

public abstract partial class AliasedLegendContentViewModel : ObservableObject
{
    private readonly IAliasedSource _source;
    private readonly IIntervalSourceRegistry _registry;
    private readonly IRemovalConfirmation _confirmation;

    [ObservableProperty]
    private string _label;

    protected AliasedLegendContentViewModel(IAliasedSource source, IIntervalSourceRegistry registry, IRemovalConfirmation confirmation)
    {
        _source = source;
        _registry = registry;
        _confirmation = confirmation;
        _label = source.Label;
    }

    public abstract string Details { get; }

    [RelayCommand]
    private void CommitRename() => _source.Label = Label;

    [RelayCommand]
    private async Task Remove()
    {
        if (await _confirmation.ConfirmAsync()) _registry.Remove(_source);
    }
}
