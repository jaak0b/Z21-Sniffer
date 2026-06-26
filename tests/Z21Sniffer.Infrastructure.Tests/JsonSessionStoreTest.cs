using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Infrastructure.Persistence;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class JsonSessionStoreTest : TempDirectoryTest
{
    private readonly JsonSessionStore _store = new();

    private static RecordingSession SampleSession()
    {
        var start = new DateTimeOffset(2026, 6, 26, 10, 0, 0, TimeSpan.Zero);
        return new RecordingSession(start,
        [
            new SensorInterval(new SensorKey(3, 5), start, start.AddSeconds(4)),
            new SensorInterval(new SensorKey(3, 5), start.AddSeconds(10), null)
        ]);
    }

    [Test]
    public void SaveJson_ThenLoadJson_RoundTripsSession()
    {
        var session = SampleSession();
        var path = Path.Combine(TempDir, "session.json");

        _store.SaveJson(session, path);
        var loaded = _store.LoadJson(path);

        Assert.That(loaded.StartedAt, Is.EqualTo(session.StartedAt));
        Assert.That(loaded.Intervals, Is.EqualTo(session.Intervals));
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
