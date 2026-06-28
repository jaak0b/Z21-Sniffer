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

        var loco = new LocoIntervalSource { Id = "loco:482", Address = 482 };
        loco.Apply(40, forward: true, maxSpeed: 28, Start);
        loco.Apply(80, forward: false, maxSpeed: 28, Start.AddSeconds(2));
        loco.Apply(0, forward: true, maxSpeed: 28, Start.AddSeconds(6));

        var current = new SystemCurrentSource { Id = "systemcurrent" };
        current.Apply(milliamps: 820, maxCurrentMilliamps: 3200, Start);
        current.Apply(milliamps: 1100, maxCurrentMilliamps: 3200, Start.AddSeconds(3));

        var trackPower = new TrackPowerSource { Id = "trackpower" };
        trackPower.Set(TrackPowerStatus.On, Start);
        trackPower.Set(TrackPowerStatus.Short, Start.AddSeconds(4));

        return new RecordingSession(Start, new IIntervalSource[] { sensor, connection, loco, current, trackPower });
    }

    private static TrackPowerSource TrackPower(RecordingSession session) =>
        session.Sources.OfType<TrackPowerSource>().Single();

    private static SystemCurrentSource Current(RecordingSession session) =>
        session.Sources.OfType<SystemCurrentSource>().Single();

    private static LocoIntervalSource Loco(RecordingSession session) =>
        session.Sources.OfType<LocoIntervalSource>().Single();

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
        Assert.That(loaded.Sources, Has.Count.EqualTo(5));
        Assert.That(loaded.Sources.OfType<FeedbackSensorSource>().Count(), Is.EqualTo(1));
        Assert.That(loaded.Sources.OfType<ConnectionSource>().Count(), Is.EqualTo(1));
        Assert.That(loaded.Sources.OfType<LocoIntervalSource>().Count(), Is.EqualTo(1));
        Assert.That(loaded.Sources.OfType<SystemCurrentSource>().Count(), Is.EqualTo(1));
        Assert.That(loaded.Sources.OfType<TrackPowerSource>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void SaveJson_ThenLoadJson_RoundTripsTrackPowerSource()
    {
        var path = Path.Combine(TempDir, "session.json");

        _store.SaveJson(SampleSession(), path);
        var trackPower = TrackPower(_store.LoadJson(path));

        Assert.That(trackPower.Intervals, Has.Count.EqualTo(2));
        Assert.That(trackPower.Intervals[0].Status, Is.EqualTo(TrackPowerStatus.On));
        Assert.That(trackPower.Intervals[0].End, Is.EqualTo(Start.AddSeconds(4)));
        Assert.That(trackPower.Intervals[1].Status, Is.EqualTo(TrackPowerStatus.Short));
    }

    [Test]
    public void SaveJson_ThenLoadJson_RoundTripsSystemCurrentSource()
    {
        var path = Path.Combine(TempDir, "session.json");

        _store.SaveJson(SampleSession(), path);
        var current = Current(_store.LoadJson(path));

        var interval = current.Intervals.Single();
        Assert.That(interval.MaxCurrentMilliamps, Is.EqualTo(3200));
        Assert.That(interval.Samples.Select(s => s.Milliamps), Is.EqualTo(new[] { 820, 1100 }));
        Assert.That(interval.Samples.Select(s => s.At), Is.EqualTo(new[] { Start, Start.AddSeconds(3) }));
    }

    [Test]
    public void SaveJson_ThenLoadJson_RoundTripsLocoSource()
    {
        var path = Path.Combine(TempDir, "session.json");

        _store.SaveJson(SampleSession(), path);
        var loco = Loco(_store.LoadJson(path));

        Assert.That(loco.Address, Is.EqualTo(482));
        Assert.That(loco.Intervals, Has.Count.EqualTo(1));
        var interval = loco.Intervals.Single();
        Assert.That(interval.MaxSpeed, Is.EqualTo(28));
        Assert.That(interval.End, Is.EqualTo(Start.AddSeconds(6)));
        Assert.That(interval.Samples.Select(s => s.Speed), Is.EqualTo(new[] { 40, 80 }));
        Assert.That(interval.Samples.Select(s => s.Forward), Is.EqualTo(new[] { true, false }));
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
    public void SaveJson_ExcludesLocalLabelAndOrderPreferences()
    {
        var path = Path.Combine(TempDir, "session.json");
        var sensor = new FeedbackSensorSource { Id = "sensor:3.5", Sensor = new SensorKey(3, 5), Label = "Renamed yard", Order = 7 };
        sensor.Apply(occupied: true, Start);

        _store.SaveJson(new RecordingSession(Start, new IIntervalSource[] { sensor }), path);
        var loaded = Sensor(_store.LoadJson(path));

        Assert.That(loaded.Label, Is.EqualTo("M3.5"), "Label is a local preference, reconstructed from the key-value store, not the portable session");
        Assert.That(loaded.Order, Is.EqualTo(0), "Order is a local preference, reseeded on load, not the portable session");
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
