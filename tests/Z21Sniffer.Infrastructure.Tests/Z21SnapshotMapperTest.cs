using CommandStation.Model;
using NUnit.Framework;
using Z21.Core.Model.EventArgs;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Infrastructure.Z21;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class Z21SnapshotMapperTest
{
    private Z21SnapshotMapper _mapper = null!;

    [SetUp]
    public void SetUp() => _mapper = new Z21SnapshotMapper();

    [Test]
    public void ToHardware_MapsTypeCodeAndFirmwareVersion()
    {
        var hardware = _mapper.ToHardware(new HardwareInfoEventArgs(529, 0x0141));

        Assert.That(hardware.TypeCode, Is.EqualTo(529));
        Assert.That(hardware.FirmwareVersion, Is.EqualTo(0x0141));
    }

    [Test]
    public void ToSystem_MapsTelemetryAndFaultFlags()
    {
        var state = new SystemState
        {
            MainCurrent = 320,
            SupplyVoltage = 15000,
            Temperature = 32,
            CentralState = new CentralState { EmergencyStop = true, TrackVoltageOff = true },
            CentralStateEx = new CentralStateEx { ShortCircuitInternal = true, PowerLost = true, HighTemperature = true }
        };

        var snapshot = _mapper.ToSystem(state);

        Assert.That(snapshot.MainCurrentMilliamps, Is.EqualTo(320));
        Assert.That(snapshot.SupplyVoltageMillivolts, Is.EqualTo(15000));
        Assert.That(snapshot.TemperatureCelsius, Is.EqualTo(32));
        Assert.That(snapshot.ShortCircuit, Is.True);
        Assert.That(snapshot.EmergencyStop, Is.True);
        Assert.That(snapshot.TrackVoltageOff, Is.True);
        Assert.That(snapshot.PowerLost, Is.True);
        Assert.That(snapshot.HighTemperature, Is.True);
    }

    [Test]
    public void ToSystem_ShortCircuitFromAnySource_IsTrue()
    {
        var external = _mapper.ToSystem(SystemStateWith(new CentralStateEx { ShortCircuitExternal = true }));
        var central = _mapper.ToSystem(new SystemState
        {
            CentralState = new CentralState { ShortCircuit = true },
            CentralStateEx = new CentralStateEx()
        });

        Assert.That(external.ShortCircuit, Is.True);
        Assert.That(central.ShortCircuit, Is.True);
    }

    [Test]
    public void ToSystem_MapsProgrammingMode()
    {
        var active = _mapper.ToSystem(new SystemState
        {
            CentralState = new CentralState { ProgrammingModeActive = true },
            CentralStateEx = new CentralStateEx()
        });

        Assert.That(active.ProgrammingMode, Is.True);
        Assert.That(_mapper.ToSystem(SystemStateWith(new CentralStateEx())).ProgrammingMode, Is.False);
    }

    [Test]
    public void ToSystem_NoFaults_AllFlagsFalse()
    {
        var snapshot = _mapper.ToSystem(SystemStateWith(new CentralStateEx()));

        Assert.That(snapshot.ShortCircuit, Is.False);
        Assert.That(snapshot.EmergencyStop, Is.False);
        Assert.That(snapshot.PowerLost, Is.False);
    }

    [Test]
    public void ToLoco_MapsAddressSpeedAndDirection()
    {
        var snapshot = _mapper.ToLoco(new LocoInfoData
        {
            LocoAddress = 3,
            LocoFunctionsData = [],
            DccSpeedMode = DccSpeedMode.Steps128,
            DecoderMode = DecoderMode.DCC,
            DrivingDirection = DrivingDirection.Forward,
            LocoSpeed = 42,
            LocoIsBusy = false,
            LocoContainedInDoubleTraction = false,
            SmartSearch = false
        });

        Assert.That(snapshot.Address, Is.EqualTo(3));
        Assert.That(snapshot.Speed, Is.EqualTo(42));
        Assert.That(snapshot.Forward, Is.True);
        Assert.That(snapshot.MaxSpeed, Is.EqualTo(126));
    }

    [Test]
    [TestCase(DccSpeedMode.Steps14, 14)]
    [TestCase(DccSpeedMode.Steps28, 28)]
    [TestCase(DccSpeedMode.Steps128, 126)]
    public void ToLoco_DerivesMaxSpeedFromSpeedMode(DccSpeedMode mode, int expectedMax)
    {
        var snapshot = _mapper.ToLoco(new LocoInfoData
        {
            LocoAddress = 3,
            LocoFunctionsData = [],
            DccSpeedMode = mode,
            DecoderMode = DecoderMode.DCC,
            DrivingDirection = DrivingDirection.Backward,
            LocoSpeed = 5,
            LocoIsBusy = false,
            LocoContainedInDoubleTraction = false,
            SmartSearch = false
        });

        Assert.That(snapshot.MaxSpeed, Is.EqualTo(expectedMax));
        Assert.That(snapshot.Forward, Is.False);
    }

    [Test]
    public void ToTurnout_MapsOutputToPosition()
    {
        Assert.That(_mapper.ToTurnout(new TurnoutInfo(5, AccessoryOutput.Output2)).Position, Is.EqualTo(TurnoutPosition.Output2));
        Assert.That(_mapper.ToTurnout(new TurnoutInfo(5, AccessoryOutput.Output1)).Position, Is.EqualTo(TurnoutPosition.Output1));
        Assert.That(_mapper.ToTurnout(new TurnoutInfo(5, null)).Position, Is.EqualTo(TurnoutPosition.Unknown));
        Assert.That(_mapper.ToTurnout(new TurnoutInfo(5, null)).Address, Is.EqualTo(5));
    }

    private static SystemState SystemStateWith(CentralStateEx ex) => new()
    {
        CentralState = new CentralState(),
        CentralStateEx = ex
    };
}
