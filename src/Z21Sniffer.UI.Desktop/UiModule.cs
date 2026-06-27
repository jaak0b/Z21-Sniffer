using Autofac;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Infrastructure;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.UI.Desktop;

public sealed class UiModule : Module
{
    private readonly string _dataDirectory;

    public UiModule(string dataDirectory) => _dataDirectory = dataDirectory;

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule(new InfrastructureModule(_dataDirectory));

        builder.RegisterType<IntervalSourceRegistry>().As<IIntervalSourceRegistry>().SingleInstance();
        builder.RegisterType<FeedbackSensorIngest>().AsSelf().SingleInstance();
        builder.RegisterType<RemovalConfirmation>().As<IRemovalConfirmation>().SingleInstance();

        builder.RegisterType<SensorIntervalChartDrawingStrategy>().Keyed<IIntervalChartDrawingStrategy>(typeof(FeedbackSensorInterval));
        builder.RegisterType<ConnectionIntervalChartDrawingStrategy>().Keyed<IIntervalChartDrawingStrategy>(typeof(ConnectionInterval));
        builder.RegisterType<LocoIntervalChartDrawingStrategy>().Keyed<IIntervalChartDrawingStrategy>(typeof(LocoInterval));
        builder.RegisterType<SensorIntervalLegendDrawingStrategy>().Keyed<IIntervalLegendDrawingStrategy>(typeof(FeedbackSensorInterval));
        builder.RegisterType<ConnectionIntervalLegendDrawingStrategy>().Keyed<IIntervalLegendDrawingStrategy>(typeof(ConnectionInterval));
        builder.RegisterType<LocoIntervalLegendDrawingStrategy>().Keyed<IIntervalLegendDrawingStrategy>(typeof(LocoInterval));
    }
}
