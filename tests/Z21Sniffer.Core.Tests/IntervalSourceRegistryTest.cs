using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class IntervalSourceRegistryTest
{
    private InMemoryKeyValueStore _store = null!;
    private IntervalSourceRegistry _registry = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryKeyValueStore();
        _registry = new IntervalSourceRegistry(_store);
    }

    [Test]
    public void GetOrCreate_NewKey_CreatesAndAddsWithThatId()
    {
        var source = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        Assert.That(source.Id, Is.EqualTo("sensor:1"));
        Assert.That(_registry.Sources, Is.EqualTo(new[] { source }));
    }

    [Test]
    public void GetOrCreate_ExistingKey_ReturnsTheSameInstance()
    {
        var first = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        var second = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        Assert.That(second, Is.SameAs(first));
        Assert.That(_registry.Sources, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetOrCreate_AssignsIncreasingOrder()
    {
        var first = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        var second = _registry.GetOrCreate<ConnectionSource>("connection");

        Assert.That(second.Order, Is.GreaterThan(first.Order));
    }

    [Test]
    public void Sources_AreReturnedOrderedByOrder()
    {
        var first = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        var second = _registry.GetOrCreate<FeedbackSensorSource>("sensor:2");
        second.Order = -1;

        Assert.That(_registry.Sources, Is.EqualTo(new IIntervalSource[] { second, first }));
    }

    [Test]
    public void Remove_DropsTheSource()
    {
        var source = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        _registry.Remove(source);

        Assert.That(_registry.Sources, Is.Empty);
    }

    [Test]
    public void Clear_RemovesAllSources()
    {
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        _registry.Clear();

        Assert.That(_registry.Sources, Is.Empty);
    }

    [Test]
    public void Load_ReplacesContentsWithGivenSources()
    {
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        var restored = new ConnectionSource { Id = "connection", Order = 0 };

        _registry.Load(new IIntervalSource[] { restored });

        Assert.That(_registry.Sources, Is.EqualTo(new IIntervalSource[] { restored }));
    }

    [Test]
    public void GetOrCreate_AfterLoad_ContinuesOrderAboveLoaded()
    {
        _registry.Load(new IIntervalSource[]
        {
            new ConnectionSource { Id = "connection" },
            new FeedbackSensorSource { Id = "sensor:9" },
        });

        var added = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        Assert.That(added.Order, Is.GreaterThan(_registry.Find("sensor:9")!.Order));
    }

    [Test]
    public void GetOrCreate_NewKey_RunsInitializerOnTheCreatedSource()
    {
        var created = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1", source => source.Sensor = new SensorKey(4, 2));

        Assert.That(created.Sensor, Is.EqualTo(new SensorKey(4, 2)));
    }

    [Test]
    public void GetOrCreate_ExistingKey_DoesNotRunInitializer()
    {
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1", source => source.Sensor = new SensorKey(4, 2));

        var ran = false;
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1", _ => ran = true);

        Assert.That(ran, Is.False);
    }

    [Test]
    public void GetOrCreate_BindsTheRegistryStoreToTheSource()
    {
        var source = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1", s => s.Sensor = new SensorKey(1, 1));

        source.Label = "Yard 3";

        Assert.That(_store.GetValue<string>("sensor:1/label"), Is.EqualTo("Yard 3"));
    }

    [Test]
    public void Find_ExistingKey_ReturnsTheSource()
    {
        var source = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        Assert.That(_registry.Find("sensor:1"), Is.SameAs(source));
    }

    [Test]
    public void Find_UnknownKey_ReturnsNull()
    {
        Assert.That(_registry.Find("sensor:1"), Is.Null);
    }

    [Test]
    public void GetOrCreate_NewKey_RaisesChanged()
    {
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void GetOrCreate_ExistingKey_DoesNotRaiseChanged()
    {
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        Assert.That(raised, Is.EqualTo(0));
    }

    [Test]
    public void Remove_RaisesChanged()
    {
        var source = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        _registry.Remove(source);

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void Clear_RaisesChanged()
    {
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        _registry.Clear();

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void Load_RaisesChanged()
    {
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        _registry.Load(new IIntervalSource[] { new ConnectionSource { Id = "connection" } });

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void SourceChanged_BubblesAsRegistryChanged()
    {
        var source = _registry.GetOrCreate<ConnectionSource>("connection");
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        source.Set(connected: true, DateTimeOffset.UnixEpoch);

        Assert.That(raised, Is.GreaterThanOrEqualTo(1));
    }
}
