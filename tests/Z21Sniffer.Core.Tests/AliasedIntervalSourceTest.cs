using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class AliasedIntervalSourceTest
{
    private sealed class StubInterval : IntervalBase
    {
    }

    private sealed class StubAliasedSource : AliasedIntervalSource<StubInterval>
    {
        protected override string DefaultLabel => "fallback";
    }

    private StubAliasedSource _source = null!;

    [SetUp]
    public void SetUp() => _source = new StubAliasedSource { Id = "stub:1" };

    [Test]
    public void Label_NoStoredValue_ReturnsDefaultLabel()
    {
        Assert.That(_source.Label, Is.EqualTo("fallback"));
    }

    [Test]
    public void Label_Set_PersistsToBoundStoreKeyedById()
    {
        var store = new InMemoryKeyValueStore();
        _source.UsePersistence(store);

        _source.Label = "Renamed";

        Assert.That(store.GetValue<string>("stub:1/label"), Is.EqualTo("Renamed"));
        Assert.That(_source.Label, Is.EqualTo("Renamed"));
    }

    [Test]
    public void Label_StoredValue_OverridesDefault()
    {
        var store = new InMemoryKeyValueStore();
        store.SetValue("stub:1/label", "Stored");
        _source.UsePersistence(store);

        Assert.That(_source.Label, Is.EqualTo("Stored"));
    }
}
