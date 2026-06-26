using Autofac;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Infrastructure;

namespace Z21Sniffer.UI.Desktop;

public sealed class UiModule : Module
{
    private readonly string _dataDirectory;

    public UiModule(string dataDirectory) => _dataDirectory = dataDirectory;

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule(new InfrastructureModule(_dataDirectory));
        builder.RegisterType<SensorLabeler>().AsSelf().SingleInstance();
    }
}
