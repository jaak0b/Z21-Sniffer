using System.ComponentModel;
using System.Globalization;
using System.Resources;
using CommunityToolkit.Mvvm.Messaging;

namespace Z21Sniffer.Presentation.Localization;

public sealed class LanguageChangedMessage;

public sealed class LocalizationService : INotifyPropertyChanged
{
    public static readonly LocalizationService Instance = new();

    private ResourceManager _manager;
    private string _currentCode = "en";

    private LocalizationService() => _manager = BuildManager("en");

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? LanguageChanged;

    public string CurrentCode => _currentCode;

    public string this[string key] => _manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public void Apply(string languageCode)
    {
        _currentCode = languageCode;
        var culture = new CultureInfo(languageCode);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        _manager = BuildManager(languageCode);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
        WeakReferenceMessenger.Default.Send(new LanguageChangedMessage());
    }

    private ResourceManager BuildManager(string languageCode)
    {
        var suffix = languageCode == "de" ? ".de" : ".en";
        return new ResourceManager(
            "Z21Sniffer.Presentation.Resources.Strings" + suffix,
            typeof(LocalizationService).Assembly);
    }
}
