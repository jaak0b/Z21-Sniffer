using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using NUnit.Framework;
using Z21Sniffer.Presentation.ViewModels;
using Z21Sniffer.UI.Desktop.Views;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public class SettingsWindowTest
{
    [AvaloniaTest]
    public void Constructor_PopulatesSourceChoices()
    {
        var window = new SettingsWindow();

        var combo = window.GetVisualDescendants().OfType<ComboBox>().FirstOrDefault()
            ?? window.FindControl<ComboBox>("SourceCombo");
        Assert.That(combo, Is.Not.Null);
        Assert.That(combo!.ItemsSource, Is.EquivalentTo(new[] { ConnectionSource.Z21, ConnectionSource.Simulation }));
    }
}
