using System.Globalization;
using Avalonia.Data;
using NUnit.Framework;
using Z21Sniffer.UI.Desktop.Controls;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public class NumericTextConverterTest
{
    private readonly NumericTextConverter _converter = new();
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    [Test]
    public void ConvertBack_EmptyText_IsZero() =>
        Assert.That(_converter.ConvertBack("", typeof(double), null, Invariant), Is.EqualTo(0d));

    [Test]
    public void ConvertBack_Whitespace_IsZero() =>
        Assert.That(_converter.ConvertBack("   ", typeof(double), null, Invariant), Is.EqualTo(0d));

    [Test]
    public void ConvertBack_Null_IsZero() =>
        Assert.That(_converter.ConvertBack(null, typeof(double), null, Invariant), Is.EqualTo(0d));

    [Test]
    public void ConvertBack_ValidNumber_ParsesIt() =>
        Assert.That(_converter.ConvertBack("1.5", typeof(double), null, Invariant), Is.EqualTo(1.5));

    [Test]
    public void ConvertBack_Garbage_LeavesTheSourceUntouched() =>
        Assert.That(_converter.ConvertBack("abc", typeof(double), null, Invariant), Is.SameAs(BindingOperations.DoNothing));

    [Test]
    public void Convert_FormatsTheDoubleWithCulture() =>
        Assert.That(_converter.Convert(0.5, typeof(string), null, Invariant), Is.EqualTo("0.5"));
}
