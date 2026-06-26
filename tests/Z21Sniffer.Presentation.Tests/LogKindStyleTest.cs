using NUnit.Framework;
using Z21Sniffer.Presentation.Logging;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class LogKindStyleTest
{
    private readonly LogKindStyle _style = new();

    [Test]
    public void ResourceKey_System_IsDanger() =>
        Assert.That(_style.ResourceKey(LogEntryKind.System), Is.EqualTo("DangerBrush"));

    [Test]
    public void ResourceKey_TrackPower_IsWarning() =>
        Assert.That(_style.ResourceKey(LogEntryKind.TrackPower), Is.EqualTo("WarningBrush"));

    [Test]
    public void ResourceKey_EveryKind_ResolvesToABrushKey()
    {
        foreach (LogEntryKind kind in Enum.GetValues<LogEntryKind>())
            Assert.That(_style.ResourceKey(kind), Does.EndWith("Brush"));
    }
}
