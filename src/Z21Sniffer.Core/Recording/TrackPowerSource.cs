using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class TrackPowerSource : IntervalSourceBase<TrackPowerInterval>
{
    public void Apply(SystemSnapshot snapshot, DateTimeOffset at) => Set(StatusOf(snapshot), at);

    public void Set(TrackPowerStatus status, DateTimeOffset at)
    {
        if (CurrentInterval is { } current && current.Status == status) return;

        CloseInterval(at, IntervalEndReason.FallingEdge);
        CreateInterval(at).Status = status;
    }

    private TrackPowerStatus StatusOf(SystemSnapshot snapshot) =>
        snapshot.ShortCircuit ? TrackPowerStatus.Short
        : snapshot.ProgrammingMode ? TrackPowerStatus.Programming
        : snapshot.TrackVoltageOff ? TrackPowerStatus.Off
        : TrackPowerStatus.On;
}
