using Autofac;
using Autofac.Features.Indexed;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Mcp;
using Z21Sniffer.Presentation.Timeline;
using Z21Sniffer.Presentation.ViewModels;
using Z21Sniffer.UI.Desktop.Views;

namespace Z21Sniffer.UI.Desktop;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new UiModule(ResolveDataDirectory()));
            var container = builder.Build();

            var window = new MainWindow();
            var picker = new DesktopFilePicker(window);

            AvaloniaSnifferApi? api = null;
            var mcpController = new KestrelMcpServerController(() => api!);

            WorkspaceViewModel? workspace = null;
            workspace = new WorkspaceViewModel(
                container.Resolve<ICommandStationConnectionFactory>(),
                container.Resolve<ISettingsStore>(),
                container.Resolve<ISessionStore>(),
                container.Resolve<IClock>(),
                container.Resolve<IIntervalSourceRegistry>(),
                container.Resolve<FeedbackSensorIngest>(),
                container.Resolve<IIndex<Type, IIntervalChartDrawingStrategy>>(),
                container.Resolve<IIndex<Type, IIntervalLegendDrawingStrategy>>(),
                mcpController,
                new AvaloniaThemeController(),
                container.Resolve<ILogTextStore>(),
                action => Dispatcher.UIThread.Post(action),
                picker.SaveJsonAsync,
                picker.OpenJsonAsync,
                picker.ExportLogAsync,
                () => new SettingsWindow { DataContext = workspace }.ShowDialog(window));

            api = new AvaloniaSnifferApi(workspace, container.Resolve<IClock>(), new SensorSummaryCalculator());
            window.DataContext = workspace;

            desktop.ShutdownRequested += async (_, _) => await mcpController.StopAsync();
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private string ResolveDataDirectory() =>
#if DEBUG
        Path.Combine(AppContext.BaseDirectory, "z21sniffer-data");
#else
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z21Sniffer");
#endif
}
