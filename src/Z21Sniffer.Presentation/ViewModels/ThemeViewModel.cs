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

    public IReadOnlyList<AppTheme> Themes { get; } = [AppTheme.Light, AppTheme.Dark];

    public AppTheme SelectedTheme
    {
        get => IsDark ? AppTheme.Dark : AppTheme.Light;
        set => IsDark = value == AppTheme.Dark;
    }

    partial void OnIsDarkChanged(bool value)
    {
        _controller.Apply(value);
        OnPropertyChanged(nameof(SelectedTheme));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Toggle() => IsDark = !IsDark;
}
