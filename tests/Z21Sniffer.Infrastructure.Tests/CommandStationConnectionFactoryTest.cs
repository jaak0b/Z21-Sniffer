using CommandStation.Transport.Udp;
using FakeItEasy;
using NUnit.Framework;
using Z21.Core;
using Z21.Core.ResponseHandler.SystemState;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Infrastructure;
using Z21Sniffer.Infrastructure.Simulation;
using Z21Sniffer.Infrastructure.Z21;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class CommandStationConnectionFactoryTest
{
    private Z21CommandStationConnection _live = null!;
    private SimulatedCommandStationConnection _simulated = null!;
    private CommandStationConnectionFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _live = new Z21CommandStationConnection(
            A.Fake<IZ21CommandStation>(), new UdpTransportOptions(), new FeedbackDecoder(), new Z21SnapshotMapper(),
            A.Fake<IBroadcastFlagsResponseHandler>());
        _simulated = new SimulatedCommandStationConnection(new SimulatedFeedbackScript());
        _factory = new CommandStationConnectionFactory(
            _live, _simulated, A.Fake<INetworkReachability>(),
            A.Fake<Microsoft.Extensions.Logging.ILogger<MonitoredCommandStationConnection>>());
    }

    [TearDown]
    public void TearDown() => _simulated.Dispose();

    [Test]
    public void Create_WhenSimulated_ReturnsSimulatedConnection() =>
        Assert.That(_factory.Create(simulated: true), Is.SameAs(_simulated));

    [Test]
    public void Create_WhenNotSimulated_ReturnsLiveConnectionWrappedInHealthMonitor() =>
        Assert.That(_factory.Create(simulated: false), Is.TypeOf<MonitoredCommandStationConnection>());
}
