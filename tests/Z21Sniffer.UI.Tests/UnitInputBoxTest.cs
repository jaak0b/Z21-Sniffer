using System.Globalization;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using NUnit.Framework;
using Z21Sniffer.UI.Desktop.Controls;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public class UnitInputBoxTest
{
    private CultureInfo _originalCulture = null!;

    [SetUp]
    public void SetUp()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    [TearDown]
    public void TearDown() => CultureInfo.CurrentCulture = _originalCulture;

    private static TextBox InnerTextBox(UnitInputBox control) =>
        control.GetLogicalDescendants().OfType<TextBox>().First();

    [AvaloniaTest]
    public void Value_SetOnControl_ShowsInInnerTextBox()
    {
        var control = new UnitInputBox { Value = 0.5 };

        Assert.That(InnerTextBox(control).Text, Is.EqualTo("0.5"));
    }

    [AvaloniaTest]
    public void EditingInnerTextBox_UpdatesValue()
    {
        var control = new UnitInputBox { Value = 0 };

        InnerTextBox(control).Text = "1.5";

        Assert.That(control.Value, Is.EqualTo(1.5));
    }

    [AvaloniaTest]
    public void Unit_IsRenderedAsSuffix()
    {
        var control = new UnitInputBox { Unit = "s" };

        var suffix = control.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Text == "s");

        Assert.That(suffix, Is.Not.Null);
    }
}
