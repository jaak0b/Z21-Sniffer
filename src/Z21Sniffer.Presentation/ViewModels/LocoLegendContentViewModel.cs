using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed partial class LocoLegendContentViewModel : ObservableObject
{
    private readonly LocoIntervalSource _source;
    private readonly IIntervalSourceRegistry _registry;
    private readonly IRemovalConfirmation _confirmation;

    [ObservableProperty]
    private string _label;

    public LocoLegendContentViewModel(LocoIntervalSource source, IIntervalSourceRegistry registry, IRemovalConfirmation confirmation)
    {
        _source = source;
        _registry = registry;
        _confirmation = confirmation;
        _label = source.Label;
    }

    public string Details => string.Format(CultureInfo.CurrentCulture, LocalizationService.Instance["LocoDetails"], _source.Address);

    [RelayCommand]
    private void CommitRename() => _source.Label = Label;

    [RelayCommand]
    private async Task Remove()
    {
        if (await _confirmation.ConfirmAsync()) _registry.Remove(_source);
    }
}
