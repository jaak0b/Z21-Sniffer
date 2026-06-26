using FakeItEasy;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Mcp;

namespace Z21Sniffer.Mcp.Tests;

[TestFixture]
public class FeedbackToolsTest
{
    private ISnifferApi _api = null!;
    private FeedbackTools _tools = null!;

    [SetUp]
    public void SetUp()
    {
        _api = A.Fake<ISnifferApi>();
        _tools = new FeedbackTools(_api);
    }

    [Test]
    public async Task GetSummaries_DelegatesToApi()
    {
        var expected = new List<SensorSummary> { new(1, 1, "M1.1", 3, 0.2, 0.04, 0.1) };
        A.CallTo(() => _api.GetSummariesAsync()).Returns(expected);

        Assert.That(await _tools.GetSummaries(), Is.SameAs(expected));
    }

    [Test]
    public async Task GetIntervals_WithModuleAndContact_PassesSensorKey()
    {
        await _tools.GetIntervals(module: 3, contact: 5, sinceSeconds: null);

        A.CallTo(() => _api.GetIntervalsAsync(new SensorKey(3, 5), null)).MustHaveHappened();
    }

    [Test]
    public async Task GetIntervals_WithoutModule_PassesNullSensor()
    {
        await _tools.GetIntervals(module: null, contact: null, sinceSeconds: 10);

        A.CallTo(() => _api.GetIntervalsAsync(null, 10)).MustHaveHappened();
    }

    [Test]
    public async Task GetIntervals_WithModuleButNoContact_PassesNullSensor()
    {
        await _tools.GetIntervals(module: 3, contact: null, sinceSeconds: null);

        A.CallTo(() => _api.GetIntervalsAsync(null, null)).MustHaveHappened();
    }

    [Test]
    public async Task Connect_DelegatesToApiAndConfirmsTarget()
    {
        var message = await _tools.Connect("10.0.0.5", 21106, simulated: false);

        A.CallTo(() => _api.ConnectAsync("10.0.0.5", 21106, false)).MustHaveHappened();
        Assert.That(message, Does.Contain("10.0.0.5").And.Contain("21106"));
    }

    [Test]
    public async Task SetTrackPower_DelegatesToApiAndReportsState()
    {
        var on = await _tools.SetTrackPower(true);
        var off = await _tools.SetTrackPower(false);

        A.CallTo(() => _api.SetTrackPowerAsync(true)).MustHaveHappened();
        A.CallTo(() => _api.SetTrackPowerAsync(false)).MustHaveHappened();
        Assert.That(on, Does.Contain("on"));
        Assert.That(off, Does.Contain("off"));
    }

    [Test]
    public async Task RenameSensor_DelegatesToApiAndEchoesName()
    {
        var message = await _tools.RenameSensor(3, 5, "Station track 2");

        A.CallTo(() => _api.RenameSensorAsync(3, 5, "Station track 2")).MustHaveHappened();
        Assert.That(message, Does.Contain("Station track 2"));
    }
}
