using NUnit.Framework;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class InMemoryKeyValueStoreTest
{
    private InMemoryKeyValueStore _store = null!;

    [SetUp]
    public void SetUp() => _store = new InMemoryKeyValueStore();

    [Test]
    public void GetValue_MissingKey_ReturnsProvidedDefault()
    {
        Assert.That(_store.GetValue("absent", 7), Is.EqualTo(7));
        Assert.That(_store.GetValue<string>("absent", "fallback"), Is.EqualTo("fallback"));
    }

    [Test]
    public void SetValue_ThenGetValue_RoundTrips()
    {
        _store.SetValue("k", 42);

        Assert.That(_store.GetValue("k", 0), Is.EqualTo(42));
    }

    [Test]
    public void SetValue_Overwrites()
    {
        _store.SetValue("k", "first");
        _store.SetValue("k", "second");

        Assert.That(_store.GetValue<string>("k"), Is.EqualTo("second"));
    }
}
