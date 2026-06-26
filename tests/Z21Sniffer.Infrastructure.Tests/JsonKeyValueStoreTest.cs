using NUnit.Framework;
using Z21Sniffer.Infrastructure.Persistence;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class JsonKeyValueStoreTest : TempDirectoryTest
{
    private string Path => System.IO.Path.Combine(TempDir, "kv.json");

    [Test]
    public void GetValue_MissingKey_ReturnsProvidedDefault()
    {
        var store = new JsonKeyValueStore(Path);

        Assert.That(store.GetValue("absent", 7), Is.EqualTo(7));
        Assert.That(store.GetValue<string>("absent", "fallback"), Is.EqualTo("fallback"));
    }

    [Test]
    public void SetValue_ThenGetValue_RoundTrips()
    {
        var store = new JsonKeyValueStore(Path);

        store.SetValue("k", 42);

        Assert.That(store.GetValue("k", 0), Is.EqualTo(42));
    }

    [Test]
    public void SetValue_PersistsToDiskAcrossInstances()
    {
        new JsonKeyValueStore(Path).SetValue("sensor:1.1/label", "Yard 3");

        var reopened = new JsonKeyValueStore(Path);

        Assert.That(reopened.GetValue<string>("sensor:1.1/label"), Is.EqualTo("Yard 3"));
    }

    [Test]
    public void SetValue_PersistsIntsAcrossInstances()
    {
        new JsonKeyValueStore(Path).SetValue("sensor:1.1/order", 5);

        Assert.That(new JsonKeyValueStore(Path).GetValue("sensor:1.1/order", -1), Is.EqualTo(5));
    }

    [Test]
    public void SetValue_CreatesMissingDirectory()
    {
        var nested = System.IO.Path.Combine(TempDir, "nested", "kv.json");

        new JsonKeyValueStore(nested).SetValue("k", 1);

        Assert.That(File.Exists(nested), Is.True);
    }
}
