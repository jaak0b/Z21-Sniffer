using CommunityToolkit.Mvvm.ComponentModel;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Presentation.Controls;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.Logging;

public sealed partial class KindToggle : ObservableObject, ISelectableOption
{
    private readonly LocalizationService _localization;

    [ObservableProperty]
    private bool _isSelected = true;

    public KindToggle(LogEntryKind kind, LocalizationService localization)
    {
        Kind = kind;
        _localization = localization;
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(nameof(Label));
    }

    public LogEntryKind Kind { get; }

    public string Label => _localization["Kind" + Kind];
}
