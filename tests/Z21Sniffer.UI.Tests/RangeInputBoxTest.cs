using System.Globalization;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using NUnit.Framework;
using Z21Sniffer.UI.Desktop.Controls;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public class RangeInputBoxTest
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

    private static IReadOnlyList<TextBox> Inputs(RangeInputBox control) =>
        control.GetLogicalDescendants().OfType<TextBox>().ToList();

    [AvaloniaTest]
    public void ClearingMinInput_SetsMinValueToZeroWithoutThrowing()
    {
        var control = new RangeInputBox { MinValue = 0.3 };

        Assert.That(() => Inputs(control)[0].Text = "", Throws.Nothing);
        Assert.That(control.MinValue, Is.EqualTo(0));
    }

    [AvaloniaTest]
    public void ClearingMaxInput_SetsMaxValueToZeroWithoutThrowing()
    {
        var control = new RangeInputBox { MaxValue = 0.5 };

        Assert.That(() => Inputs(control)[1].Text = "", Throws.Nothing);
        Assert.That(control.MaxValue, Is.EqualTo(0));
    }

    [AvaloniaTest]
    public void EditingMaxInput_UpdatesMaxValue()
    {
        var control = new RangeInputBox { MaxValue = 0 };

        Inputs(control)[1].Text = "0.5";

        Assert.That(control.MaxValue, Is.EqualTo(0.5));
    }

    [AvaloniaTest]
    public void SeparatorAndUnit_AreRendered()
    {
        var control = new RangeInputBox { Separator = "to", Unit = "s" };

        var texts = control.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();

        Assert.That(texts, Does.Contain("to").And.Contains("s"));
    }
}
