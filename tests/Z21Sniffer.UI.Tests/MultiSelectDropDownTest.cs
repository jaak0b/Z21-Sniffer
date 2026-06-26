using Avalonia.Headless.NUnit;
using CommunityToolkit.Mvvm.ComponentModel;
using NUnit.Framework;
using Z21Sniffer.Presentation.Controls;
using Z21Sniffer.UI.Desktop.Controls;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public partial class MultiSelectDropDownTest
{
    private sealed partial class FakeOption : ObservableObject, ISelectableOption
    {
        [ObservableProperty]
        private bool _isSelected = true;

        public FakeOption(string label) => Label = label;

        public string Label { get; }
    }

    [AvaloniaTest]
    public void DeselectAll_ClearsEverySelection()
    {
        var options = new[] { new FakeOption("a"), new FakeOption("b") };
        var control = new MultiSelectDropDown { ItemsSource = options };

        control.DeselectAll();

        Assert.That(options.All(o => !o.IsSelected), Is.True);
    }

    [AvaloniaTest]
    public void SelectAll_SetsEverySelection()
    {
        var options = new[] { new FakeOption("a") { IsSelected = false }, new FakeOption("b") { IsSelected = false } };
        var control = new MultiSelectDropDown { ItemsSource = options };

        control.SelectAll();

        Assert.That(options.All(o => o.IsSelected), Is.True);
    }
}
