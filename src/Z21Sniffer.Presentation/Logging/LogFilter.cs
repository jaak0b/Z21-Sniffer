namespace Z21Sniffer.Presentation.Logging;

public sealed class LogFilter
{
    public bool Matches(LogEntry entry, IReadOnlySet<LogEntryKind> enabledKinds, string searchText) =>
        enabledKinds.Contains(entry.Kind)
        && (string.IsNullOrWhiteSpace(searchText)
            || entry.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase));
}
