using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed partial class ThemeViewModel : ObservableObject
{
    private readonly IThemeController _controller;

    [ObservableProperty]
    private bool _isDark;

    public ThemeViewModel(IThemeController controller, bool isDark)
    {
        _controller = controller;
        _isDark = isDark;
        _controller.Apply(isDark);
    }

    public event EventHandler? Changed;

    partial void OnIsDarkChanged(bool value)
    {
        _controller.Apply(value);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Toggle() => IsDark = !IsDark;
}
