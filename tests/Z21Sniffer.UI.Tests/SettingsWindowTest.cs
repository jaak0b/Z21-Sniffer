using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using FakeItEasy;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.ViewModels;
using Z21Sniffer.UI.Desktop.Views;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public class SettingsWindowTest
{
    private sealed class StubClock : IClock
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
    }

    private static WorkspaceViewModel BuildWorkspace(string language = "en")
    {
        var settings = A.Fake<ISettingsStore>();
        A.CallTo(() => settings.Load()).Returns(new AppSettings("192.168.0.111", 21105, language));
        return WorkspaceFactory.Build(settings, new StubClock());
    }

    [AvaloniaTest]
    public void Constructor_PopulatesSourceChoices()
    {
        var window = new SettingsWindow();

        var combo = window.FindControl<ComboBox>("SourceCombo");
        Assert.That(combo, Is.Not.Null);
        Assert.That(combo!.ItemsSource, Is.EquivalentTo(new[] { ConnectionSourceType.Z21, ConnectionSourceType.Simulation }));
    }

    [AvaloniaTest]
    public void AppearanceCombos_OfferTwoChoicesEach()
    {
        var window = new SettingsWindow { DataContext = BuildWorkspace() };

        var theme = window.FindControl<ComboBox>("ThemeCombo");
        var language = window.FindControl<ComboBox>("LanguageCombo");
        Assert.That(theme!.ItemCount, Is.EqualTo(2));
        Assert.That(language!.ItemCount, Is.EqualTo(2));
    }

    [AvaloniaTest]
    public void ThemeCombo_ReflectsViewModelDarkState()
    {
        var workspace = BuildWorkspace();
        var window = new SettingsWindow { DataContext = workspace };

        workspace.Theme.IsDark = true;

        var theme = window.FindControl<ComboBox>("ThemeCombo");
        Assert.That(theme!.SelectedItem, Is.EqualTo(AppTheme.Dark));
    }

    [AvaloniaTest]
    public void CaptureTrainDataWarning_IsHiddenUntilCheckboxIsChecked()
    {
        var workspace = BuildWorkspace();
        var window = new SettingsWindow { DataContext = workspace };

        var warning = window.FindControl<TextBlock>("CaptureTrainDataWarning");
        Assert.That(warning!.IsVisible, Is.False);

        workspace.CaptureTrainData = true;

        Assert.That(warning.IsVisible, Is.True);
    }

    [AvaloniaTest]
    public void CaptureTrainDataCheckbox_TogglesViewModelFlag()
    {
        var workspace = BuildWorkspace();
        var window = new SettingsWindow { DataContext = workspace };

        var checkbox = window.FindControl<CheckBox>("CaptureTrainDataCheck");
        checkbox!.IsChecked = true;

        Assert.That(workspace.CaptureTrainData, Is.True);
    }

    [AvaloniaTest]
    public void ResetButtons_AreWiredToTheirCommands()
    {
        var workspace = BuildWorkspace();
        var window = new SettingsWindow { DataContext = workspace };

        var names = window.FindControl<Button>("ResetRowNamesButton");
        var order = window.FindControl<Button>("ResetRowOrderButton");

        Assert.That(names, Is.Not.Null);
        Assert.That(order, Is.Not.Null);
        Assert.That(names!.Command, Is.SameAs(workspace.ResetAliasesCommand));
        Assert.That(order!.Command, Is.SameAs(workspace.ResetOrderCommand));
    }

    [AvaloniaTest]
    public void LanguageCombo_SelectingGerman_SwitchesLanguage()
    {
        var workspace = BuildWorkspace();
        var window = new SettingsWindow { DataContext = workspace };

        var language = window.FindControl<ComboBox>("LanguageCombo");
        language!.SelectedItem = AppLanguage.German;

        Assert.That(workspace.Localization.CurrentCode, Is.EqualTo("de"));
        workspace.Localization.Apply("en");
    }
}
