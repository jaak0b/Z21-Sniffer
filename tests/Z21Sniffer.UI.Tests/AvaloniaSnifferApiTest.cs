using Avalonia.Headless.NUnit;
using FakeItEasy;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.ViewModels;
using Z21Sniffer.UI.Desktop;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public class AvaloniaSnifferApiTest
{
    private sealed class StubClock : IClock
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
    }

    private static WorkspaceContext BuildWorkspace()
    {
        var settings = A.Fake<ISettingsStore>();
        A.CallTo(() => settings.Load()).Returns(new AppSettings("192.168.0.111", 21105, "en"));
        return WorkspaceFactory.BuildContext(settings, new StubClock());
    }

    [AvaloniaTest]
    public async Task ListSensorsAsync_ReflectsSourcesAndOccupancy()
    {
        var workspace = BuildWorkspace();
        workspace.Ingest.Apply([new SensorState(new SensorKey(1, 1), Occupied: true)], DateTimeOffset.UnixEpoch);
        var api = new AvaloniaSnifferApi(workspace.Vm, new StubClock(), new SensorSummaryCalculator());

        var sensors = await api.ListSensorsAsync();

        var sensor = sensors.Single();
        Assert.That((sensor.Module, sensor.Contact), Is.EqualTo((1, 1)));
        Assert.That(sensor.Occupied, Is.True);
    }

    [AvaloniaTest]
    public async Task GetSummariesAsync_CountsRecordedIntervals()
    {
        var workspace = BuildWorkspace();
        workspace.Ingest.Apply([new SensorState(new SensorKey(2, 3), Occupied: true)], DateTimeOffset.UnixEpoch);
        var api = new AvaloniaSnifferApi(workspace.Vm, new StubClock { Now = DateTimeOffset.UnixEpoch.AddSeconds(5) },
            new SensorSummaryCalculator());

        var summaries = await api.GetSummariesAsync();

        var summary = summaries.Single();
        Assert.That(summary.OnCount, Is.EqualTo(1));
        Assert.That(summary.TotalOnSeconds, Is.EqualTo(5).Within(1e-6));
    }
}
