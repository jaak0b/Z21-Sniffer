using System.Globalization;

namespace Z21Sniffer.Presentation.Logging;

public sealed class LogTextFormatter
{
    public string Format(IEnumerable<LogEntry> entries) =>
        string.Join(Environment.NewLine, entries.Select(Line));

    private string Line(LogEntry entry) =>
        $"{entry.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)}  {entry.Kind}  {entry.Message}";
}
