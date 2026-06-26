using NUnit.Framework;
using Z21Sniffer.Presentation.Logging;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class LogTextFormatterTest
{
    private readonly LogTextFormatter _formatter = new();

    [Test]
    public void Format_OneEntry_HasTimeKindAndMessage()
    {
        var line = _formatter.Format([
            new LogEntry(DateTimeOffset.UnixEpoch, LogEntryKind.System, "320 mA")
        ]);

        Assert.That(line, Does.Contain("System"));
        Assert.That(line, Does.Contain("320 mA"));
        Assert.That(line, Does.Contain("00:00:00.000"));
    }

    [Test]
    public void Format_MultipleEntries_OneLineEach()
    {
        var text = _formatter.Format([
            new LogEntry(DateTimeOffset.UnixEpoch, LogEntryKind.Sensor, "a"),
            new LogEntry(DateTimeOffset.UnixEpoch, LogEntryKind.Sensor, "b")
        ]);

        Assert.That(text.Split(Environment.NewLine), Has.Length.EqualTo(2));
    }
}
