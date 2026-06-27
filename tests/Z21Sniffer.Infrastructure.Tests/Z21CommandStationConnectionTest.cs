using CommandStation;
using CommandStation.Model;
using CommandStation.Transport;
using CommandStation.Transport.Udp;
using FakeItEasy;
using NUnit.Framework;
using Z21.Core;
using Z21.Core.Command;
using Z21.Core.Command.SystemState;
using Z21.Core.Model;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Infrastructure.Z21;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class Z21CommandStationConnectionTest
{
    private IZ21CommandStation _station = null!;
    private UdpTransportOptions _options = null!;
    private Z21CommandStationConnection _connection = null!;

    [SetUp]
    public void SetUp()
    {
        _station = A.Fake<IZ21CommandStation>();
        A.CallTo(() => _station.Commands).Returns(A.Fake<IZ21CommandFactory>());
        _options = new UdpTransportOptions();
        _connection = new Z21CommandStationConnection(_station, _options, new FeedbackDecoder(), new Z21SnapshotMapper());
    }

    [Test]
    public void FeedbackChanged_RaisesDecodedFeedbackReceived()
    {
        IReadOnlyList<SensorState>? received = null;
        _connection.FeedbackReceived += (_, states) => received = states;

        _station.FeedbackChanged += Raise.With(_station, new FeedbackData(0, [0b0000_0001]));

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Single(s => s.Sensor == new SensorKey(1, 1)).Occupied, Is.True);
    }

    [Test]
    public void SystemStateReceived_RaisesMappedSnapshot()
    {
        SystemSnapshot? snapshot = null;
        _connection.SystemStateReceived += (_, s) => snapshot = s;

        _station.SystemStateReceived += Raise.With(_station, new SystemState
        {
            MainCurrent = 250,
            CentralState = new CentralState { EmergencyStop = true },
            CentralStateEx = new CentralStateEx { ShortCircuitExternal = true }
        });

        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot!.MainCurrentMilliamps, Is.EqualTo(250));
        Assert.That(snapshot.EmergencyStop, Is.True);
        Assert.That(snapshot.ShortCircuit, Is.True);
    }

    [Test]
    public void LocoInfoReceived_RaisesMappedSnapshot()
    {
        LocoSnapshot? snapshot = null;
        _connection.LocoInfoReceived += (_, s) => snapshot = s;

        _station.LocoInfoReceived += Raise.With(_station, new LocoInfoData
        {
            LocoAddress = 7,
            LocoFunctionsData = [],
            DccSpeedMode = DccSpeedMode.Steps128,
            DecoderMode = DecoderMode.DCC,
            DrivingDirection = DrivingDirection.Forward,
            LocoSpeed = 50,
            LocoIsBusy = false,
            LocoContainedInDoubleTraction = false,
            SmartSearch = false
        });

        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot!.Address, Is.EqualTo(7));
        Assert.That(snapshot.Speed, Is.EqualTo(50));
        Assert.That(snapshot.Forward, Is.True);
    }

    [Test]
    public void TurnoutInfoReceived_RaisesMappedSnapshot()
    {
        TurnoutSnapshot? snapshot = null;
        _connection.TurnoutInfoReceived += (_, s) => snapshot = s;

        _station.TurnoutInfoReceived += Raise.With(_station, new TurnoutInfo(9, AccessoryOutput.Output2));

        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot!.Address, Is.EqualTo(9));
        Assert.That(snapshot.Position, Is.EqualTo(TurnoutPosition.Output2));
    }

    [Test]
    public void TrackPowerChanged_RaisesTrackPowerChanged()
    {
        bool? power = null;
        _connection.TrackPowerChanged += (_, on) => power = on;

        _station.TrackPowerChanged += Raise.With(_station, true);

        Assert.That(power, Is.True);
    }

    [Test]
    public void ConnectionChanged_RaisesConnectionChanged()
    {
        bool? connected = null;
        _connection.ConnectionChanged += (_, c) => connected = c;

        _station.ConnectionChanged += Raise.With<ConnectionChangedEventArgs>(_station, new(true));

        Assert.That(connected, Is.True);
    }

    [Test]
    public void IsConnected_ReflectsStation()
    {
        A.CallTo(() => _station.IsConnected).Returns(true);

        Assert.That(_connection.IsConnected, Is.True);
    }

    [Test]
    public async Task ConnectAsync_SetsEndpointConnectsAndEnablesBroadcasts()
    {
        await _connection.ConnectAsync("10.0.0.5", 21106);

        Assert.That(_options.RemoteEndPoint.Address.ToString(), Is.EqualTo("10.0.0.5"));
        Assert.That(_options.RemoteEndPoint.Port, Is.EqualTo(21106));
        A.CallTo(() => _station.ConnectAsync()).MustHaveHappened();
        A.CallTo(() => _station.SendCommandsAsync(A<IZ21Command[]>._)).MustHaveHappened();
    }

    [Test]
    public async Task ConnectAsync_SubscribesToAllLocoInfoBroadcasts()
    {
        await _connection.ConnectAsync("10.0.0.5", 21106);

        A.CallTo(() => _station.Commands.Create<SetBroadcastFlagsCommand>(
                A<uint[]>.That.Matches(flags => flags[0] == (
                    Z21BroadcastFlags.RmBusDataChangedMessages
                    | Z21BroadcastFlags.SystemStateDataChangedMessages
                    | Z21BroadcastFlags.DriveAndSwitchingMessages
                    | Z21BroadcastFlags.LocoInfoChangedMessages))))
            .MustHaveHappened();
    }

    [Test]
    public async Task ConnectAsync_SeedsInitialFeedbackForBothGroups()
    {
        await _connection.ConnectAsync("10.0.0.5", 21106);

        A.CallTo(() => _station.RequestFeedbackAsync(0)).MustHaveHappened();
        A.CallTo(() => _station.RequestFeedbackAsync(1)).MustHaveHappened();
    }

    [Test]
    public async Task ConnectAsync_SeedsSystemState()
    {
        await _connection.ConnectAsync("10.0.0.5", 21106);

        A.CallTo(() => _station.RequestSystemStateAsync()).MustHaveHappened();
    }

    [Test]
    public async Task SetTrackPowerAsync_True_TurnsStationPowerOn()
    {
        await _connection.SetTrackPowerAsync(true);

        A.CallTo(() => _station.TrackPowerOnAsync()).MustHaveHappened();
    }

    [Test]
    public async Task SetTrackPowerAsync_False_TurnsStationPowerOff()
    {
        await _connection.SetTrackPowerAsync(false);

        A.CallTo(() => _station.TrackPowerOffAsync()).MustHaveHappened();
    }

    [Test]
    public async Task DisconnectAsync_DisconnectsStation()
    {
        await _connection.DisconnectAsync();

        A.CallTo(() => _station.DisconnectAsync()).MustHaveHappened();
    }
}
