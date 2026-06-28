using Autofac;
using Serilog.Extensions.Autofac.DependencyInjection;
using Z21.Autofac;
using Z21.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Infrastructure.Logging;
using Z21Sniffer.Infrastructure.Persistence;
using Z21Sniffer.Infrastructure.Simulation;
using Z21Sniffer.Infrastructure.Z21;

namespace Z21Sniffer.Infrastructure;

public sealed class InfrastructureModule : Module
{
    private readonly string _dataDirectory;

    public InfrastructureModule(string dataDirectory) => _dataDirectory = dataDirectory;

    protected override void Load(ContainerBuilder builder)
    {
        builder.AddZ21(optionsConfiguration: o => o.BroadcastFlags =
                                                  [
                                                      Z21BroadcastFlags.RmBusDataChangedMessages,
                                                      Z21BroadcastFlags.SystemStateDataChangedMessages,
                                                      Z21BroadcastFlags.DriveAndSwitchingMessages,
                                                      Z21BroadcastFlags.LocoInfoChangedMessages,
                                                  ]);

        var paths = new AppPaths(_dataDirectory);
        builder.RegisterInstance(paths).As<IAppPaths>().SingleInstance();
        builder.RegisterSerilog(new SerilogSetup().Create(paths.LogsDirectory));

        builder.RegisterType<SystemClock>().As<IClock>().SingleInstance();
        builder.RegisterType<FeedbackDecoder>().AsSelf().SingleInstance();
        builder.RegisterType<Z21SnapshotMapper>().AsSelf().SingleInstance();
        builder.RegisterType<SimulatedFeedbackScript>().AsSelf().SingleInstance();

        builder.RegisterType<JsonSessionStore>().As<ISessionStore>().SingleInstance();
        builder.RegisterType<FileLogTextStore>().As<ILogTextStore>().SingleInstance();
        builder.Register(c => new JsonSettingsStore(c.Resolve<IAppPaths>().SettingsFile))
            .As<ISettingsStore>().SingleInstance();
        builder.Register(c => new JsonKeyValueStore(c.Resolve<IAppPaths>().KeyValueFile))
            .As<IKeyValueStore>().SingleInstance();
        builder.Register(_ => new JsonStationCurrentLimits(Path.Combine(AppContext.BaseDirectory, "hardware-current.json")))
            .As<IStationCurrentLimits>().SingleInstance();

        builder.RegisterType<PingReachability>().As<INetworkReachability>().SingleInstance();
        builder.RegisterType<Z21CommandStationConnection>().AsSelf().SingleInstance();
        builder.RegisterType<SimulatedCommandStationConnection>().AsSelf().SingleInstance();
        builder.RegisterType<CommandStationConnectionFactory>()
            .As<ICommandStationConnectionFactory>().SingleInstance();
    }
}
