using System.Globalization;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class AccessoryBarText
{
    public string Describe(string label, int address, TurnoutPosition position, TimeSpan duration)
    {
        var addr = $"A{address}";
        var prefix = label == addr ? addr : $"{label} ({addr})";
        var state = LocalizationService.Instance[position == TurnoutPosition.Output2 ? "AccessoryPosition2" : "AccessoryPosition1"];
        var seconds = duration.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
        return $"{prefix} · {state} · {seconds} s";
    }
}
