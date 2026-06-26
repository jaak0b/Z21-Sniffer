using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Mcp;

public sealed class KestrelMcpServerController : IMcpServerController
{
    private readonly Func<ISnifferApi> _apiFactory;
    private WebApplication? _app;

    public KestrelMcpServerController(Func<ISnifferApi> apiFactory) => _apiFactory = apiFactory;

    public bool IsRunning => _app is not null;

    public string? Url { get; private set; }

    public async Task StartAsync(int port)
    {
        if (_app is not null) return;

        var api = _apiFactory() ?? throw new InvalidOperationException("Sniffer API is not available yet.");
        var url = $"http://127.0.0.1:{port}";
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddSingleton(api);
        builder.Services.AddMcpServer().WithHttpTransport().WithTools<FeedbackTools>();

        var app = builder.Build();
        app.Urls.Clear();
        app.Urls.Add(url);
        app.MapMcp();
        await app.StartAsync();

        _app = app;
        Url = url;
    }

    public async Task StopAsync()
    {
        if (_app is null) return;

        await _app.StopAsync();
        await _app.DisposeAsync();
        _app = null;
        Url = null;
    }
}
