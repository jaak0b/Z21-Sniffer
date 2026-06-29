using Autofac;
using Autofac.Features.Indexed;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Mcp;
using Z21Sniffer.Presentation.Timeline;
using Z21Sniffer.Presentation.Updates;
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

            var log = container.Resolve<ILoggerFactory>().CreateLogger("Z21Sniffer");
            log.LogInformation("Z21Sniffer starting.");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                log.LogCritical(e.ExceptionObject as Exception, "Unhandled AppDomain exception.");
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                log.LogError(e.Exception, "Unobserved task exception.");
                e.SetObserved();
            };

            _ = new UpdateCheck(new VelopackAppUpdater(), new AppLog(log)).RunAsync();

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
                container.Resolve<IStationCurrentLimits>(),
                action => Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Unhandled exception in a UI-dispatched handler.");
                    }
                }),
                picker.SaveJsonAsync,
                picker.OpenJsonAsync,
                () => new SettingsWindow { DataContext = workspace }.ShowDialog(window));

            api = new AvaloniaSnifferApi(workspace, workspace.TimelineClock, new SensorSummaryCalculator());
            window.DataContext = workspace;

            var shutdownCleanupDone = false;
            desktop.ShutdownRequested += async (_, e) =>
            {
                if (shutdownCleanupDone)
                    return;

                e.Cancel = true;
                try
                {
                    if (workspace.Connection.IsConnected)
                        await workspace.Connection.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error disconnecting from the command station during shutdown.");
                }

                await mcpController.StopAsync();
                shutdownCleanupDone = true;
                desktop.Shutdown();
            };
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
