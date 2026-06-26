using NUnit.Framework;
using Z21Sniffer.Presentation.Controls;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Logging;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class KindToggleTest
{
    [SetUp]
    public void SetUp() => LocalizationService.Instance.Apply("en");

    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    [Test]
    public void IsSelectableOption() =>
        Assert.That(new KindToggle(LogEntryKind.System, LocalizationService.Instance), Is.InstanceOf<ISelectableOption>());

    [Test]
    public void Label_IsLocalizedKindName()
    {
        var toggle = new KindToggle(LogEntryKind.System, LocalizationService.Instance);

        Assert.That(toggle.Label, Is.EqualTo("System"));
    }

    [Test]
    public void Label_UpdatesOnLanguageChange()
    {
        var toggle = new KindToggle(LogEntryKind.Sensor, LocalizationService.Instance);
        var raised = false;
        toggle.PropertyChanged += (_, e) => raised |= e.PropertyName == nameof(KindToggle.Label);

        LocalizationService.Instance.Apply("de");

        Assert.That(toggle.Label, Is.EqualTo("Rückmelder"));
        Assert.That(raised, Is.True);
    }
}
