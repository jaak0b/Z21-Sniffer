using CommandStation.Model;
using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Infrastructure.Z21;

public sealed class Z21SnapshotMapper
{
    public SystemSnapshot ToSystem(SystemState state) => new(
        state.MainCurrent,
        state.SupplyVoltage,
        state.Temperature,
        state.CentralState.ShortCircuit || state.CentralStateEx.ShortCircuitInternal || state.CentralStateEx.ShortCircuitExternal,
        state.CentralState.EmergencyStop,
        state.CentralState.TrackVoltageOff,
        state.CentralState.ProgrammingModeActive,
        state.CentralStateEx.PowerLost,
        state.CentralStateEx.HighTemperature);

    public LocoSnapshot ToLoco(LocoInfoData loco) => new(
        loco.LocoAddress,
        loco.LocoSpeed,
        loco.DrivingDirection == DrivingDirection.Forward,
        MaxSpeedFor(loco.DccSpeedMode));

    private int MaxSpeedFor(DccSpeedMode mode) => mode switch
    {
        DccSpeedMode.Steps14 => 14,
        DccSpeedMode.Steps28 => 28,
        _ => 126
    };

    public TurnoutSnapshot ToTurnout(TurnoutInfo turnout) => new(
        turnout.AccessoryAddress,
        turnout.Output switch
        {
            AccessoryOutput.Output2 => TurnoutPosition.Output2,
            AccessoryOutput.Output1 => TurnoutPosition.Output1,
            _ => TurnoutPosition.Unknown
        });
}
