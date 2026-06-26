using Autofac;
using NUnit.Framework;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Infrastructure;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class InfrastructureModuleTest : TempDirectoryTest
{
    private IContainer _container = null!;

    [SetUp]
    public void BuildContainer()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(new InfrastructureModule(TempDir));
        _container = builder.Build();
    }

    [TearDown]
    public void DisposeContainer() => _container.Dispose();

    [Test]
    public void Resolves_CommandStationConnectionFactory() =>
        Assert.That(_container.Resolve<ICommandStationConnectionFactory>(), Is.Not.Null);

    [Test]
    public void Resolves_SettingsStoreSessionStoreAndClock()
    {
        Assert.That(_container.Resolve<ISettingsStore>(), Is.Not.Null);
        Assert.That(_container.Resolve<ISessionStore>(), Is.Not.Null);
        Assert.That(_container.Resolve<IClock>(), Is.Not.Null);
    }

    [Test]
    public void Factory_ProducesDistinctLiveAndSimulatedConnections()
    {
        var factory = _container.Resolve<ICommandStationConnectionFactory>();

        Assert.That(factory.Create(simulated: true), Is.Not.SameAs(factory.Create(simulated: false)));
    }
}
