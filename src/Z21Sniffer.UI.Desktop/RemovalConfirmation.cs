using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.UI.Desktop.Views;

namespace Z21Sniffer.UI.Desktop;

public sealed class RemovalConfirmation : IRemovalConfirmation
{
    public Task<bool> ConfirmAsync()
    {
        var loc = LocalizationService.Instance;
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner is null) return Task.FromResult(false);

        return new ConfirmDialog($"{loc["ConfirmRemove"]}?", loc["Yes"], loc["No"]).ShowDialog<bool>(owner);
    }
}
