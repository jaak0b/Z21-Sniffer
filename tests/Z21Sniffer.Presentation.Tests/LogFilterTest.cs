using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Presentation.Logging;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class LogFilterTest
{
    private readonly LogFilter _filter = new();

    private static LogEntry Entry(LogEntryKind kind, string message) =>
        new(DateTimeOffset.UnixEpoch, kind, message);

    [Test]
    public void Matches_KindEnabledAndNoSearch_True()
    {
        var result = _filter.Matches(Entry(LogEntryKind.System, "anything"),
            new HashSet<LogEntryKind> { LogEntryKind.System }, "");

        Assert.That(result, Is.True);
    }

    [Test]
    public void Matches_KindDisabled_False()
    {
        var result = _filter.Matches(Entry(LogEntryKind.System, "anything"),
            new HashSet<LogEntryKind> { LogEntryKind.Sensor }, "");

        Assert.That(result, Is.False);
    }

    [Test]
    public void Matches_SearchCaseInsensitiveSubstring_True()
    {
        var result = _filter.Matches(Entry(LogEntryKind.Sensor, "Yard 3 occupied"),
            new HashSet<LogEntryKind> { LogEntryKind.Sensor }, "yard");

        Assert.That(result, Is.True);
    }

    [Test]
    public void Matches_SearchNotPresent_False()
    {
        var result = _filter.Matches(Entry(LogEntryKind.Sensor, "Yard 3 occupied"),
            new HashSet<LogEntryKind> { LogEntryKind.Sensor }, "platform");

        Assert.That(result, Is.False);
    }
}
