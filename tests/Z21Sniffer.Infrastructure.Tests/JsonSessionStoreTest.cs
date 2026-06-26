using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Infrastructure.Persistence;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class JsonSessionStoreTest : TempDirectoryTest
{
    private readonly JsonSessionStore _store = new();
    private static readonly DateTimeOffset Start = new(2026, 6, 26, 10, 0, 0, TimeSpan.Zero);

    private static RecordingSession SampleSession()
    {
        var sensor = new FeedbackSensorSource { Id = "sensor:3.5", Sensor = new SensorKey(3, 5) };
        sensor.Apply(occupied: true, Start);
        sensor.Apply(occupied: false, Start.AddSeconds(4));
        sensor.Apply(occupied: true, Start.AddSeconds(10));

        var connection = new ConnectionSource { Id = "connection" };
        connection.Set(connected: true, Start);
        connection.Set(connected: false, Start.AddSeconds(5));

        return new RecordingSession(Start, new IIntervalSource[] { sensor, connection });
    }

    private static FeedbackSensorSource Sensor(RecordingSession session) =>
        session.Sources.OfType<FeedbackSensorSource>().Single();

    private static ConnectionSource Connection(RecordingSession session) =>
        session.Sources.OfType<ConnectionSource>().Single();

    [Test]
    public void SaveJson_ThenLoadJson_RoundTripsStartedAtAndSourceTypes()
    {
        var path = Path.Combine(TempDir, "session.json");

        _store.SaveJson(SampleSession(), path);
        var loaded = _store.LoadJson(path);

        Assert.That(loaded.StartedAt, Is.EqualTo(Start));
        Assert.That(loaded.Sources, Has.Count.EqualTo(2));
        Assert.That(loaded.Sources.OfType<FeedbackSensorSource>().Count(), Is.EqualTo(1));
        Assert.That(loaded.Sources.OfType<ConnectionSource>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void SaveJson_ThenLoadJson_RoundTripsSensorSource()
    {
        var path = Path.Combine(TempDir, "session.json");

        _store.SaveJson(SampleSession(), path);
        var sensor = Sensor(_store.LoadJson(path));

        Assert.That(sensor.Id, Is.EqualTo("sensor:3.5"));
        Assert.That(sensor.Sensor, Is.EqualTo(new SensorKey(3, 5)));
        Assert.That(sensor.Label, Is.EqualTo("M3.5"), "Label is a local KV preference, not part of the portable session");
        Assert.That(sensor.Intervals, Has.Count.EqualTo(2));
        Assert.That(sensor.Intervals[0].End, Is.EqualTo(Start.AddSeconds(4)));
        Assert.That(sensor.Intervals[0].EndReason, Is.EqualTo(IntervalEndReason.FallingEdge));
        Assert.That(sensor.Intervals[1].IsOpen, Is.True);
    }

    [Test]
    public void SaveJson_ThenLoadJson_RoundTripsConnectionSource()
    {
        var path = Path.Combine(TempDir, "session.json");

        _store.SaveJson(SampleSession(), path);
        var connection = Connection(_store.LoadJson(path));

        Assert.That(connection.Intervals, Has.Count.EqualTo(2));
        Assert.That(connection.Intervals[0].Connected, Is.True);
        Assert.That(connection.Intervals[0].End, Is.EqualTo(Start.AddSeconds(5)));
        Assert.That(connection.Intervals[1].Connected, Is.False);
        Assert.That(connection.Intervals[1].IsOpen, Is.True);
    }

    [Test]
    public void SaveJson_CreatesMissingDirectory()
    {
        var path = Path.Combine(TempDir, "nested", "deep", "session.json");

        _store.SaveJson(SampleSession(), path);

        Assert.That(File.Exists(path), Is.True);
    }

    [Test]
    public void LoadJson_WithNullContent_Throws()
    {
        var path = Path.Combine(TempDir, "null.json");
        File.WriteAllText(path, "null");

        var exception = Assert.Throws<InvalidDataException>(() => _store.LoadJson(path));

        Assert.That(exception!.Message, Does.Contain(path));
    }
}
