using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed partial class McpServerViewModel : ObservableObject
{
    private readonly IMcpServerController _controller;

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string? _url;

    [ObservableProperty]
    private string? _statusText;

    public McpServerViewModel(IMcpServerController controller, int port)
    {
        _controller = controller;
        _port = port;
    }

    public event EventHandler? Started;

    [RelayCommand]
    private async Task Toggle()
    {
        try
        {
            if (_controller.IsRunning)
            {
                await _controller.StopAsync();
            }
            else
            {
                await _controller.StartAsync(Port);
            }

            StatusText = null;
        }
        catch (Exception exception)
        {
            StatusText = exception.Message;
        }

        IsRunning = _controller.IsRunning;
        Url = _controller.Url;
        if (IsRunning) Started?.Invoke(this, EventArgs.Empty);
    }
}
