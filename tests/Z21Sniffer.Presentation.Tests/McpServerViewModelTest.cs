using NUnit.Framework;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class McpServerViewModelTest
{
    private sealed class FakeController : IMcpServerController
    {
        public int? StartedOnPort { get; private set; }
        public int StopCount { get; private set; }
        public bool IsRunning { get; private set; }
        public string? Url { get; private set; }

        public Task StartAsync(int port)
        {
            StartedOnPort = port;
            IsRunning = true;
            Url = $"http://127.0.0.1:{port}";
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            StopCount++;
            IsRunning = false;
            Url = null;
            return Task.CompletedTask;
        }
    }

    private FakeController _controller = null!;
    private McpServerViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _controller = new FakeController();
        _vm = new McpServerViewModel(_controller, port: 8731);
    }

    [Test]
    public void Constructor_SeedsPort()
    {
        Assert.That(_vm.Port, Is.EqualTo(8731));
        Assert.That(_vm.IsRunning, Is.False);
    }

    [Test]
    public async Task Toggle_WhenStopped_StartsOnConfiguredPortAndReflectsState()
    {
        _vm.Port = 9000;

        await _vm.ToggleCommand.ExecuteAsync(null);

        Assert.That(_controller.StartedOnPort, Is.EqualTo(9000));
        Assert.That(_vm.IsRunning, Is.True);
        Assert.That(_vm.Url, Is.EqualTo("http://127.0.0.1:9000"));
    }

    [Test]
    public async Task Toggle_WhenRunning_Stops()
    {
        await _vm.ToggleCommand.ExecuteAsync(null);

        await _vm.ToggleCommand.ExecuteAsync(null);

        Assert.That(_controller.StopCount, Is.EqualTo(1));
        Assert.That(_vm.IsRunning, Is.False);
        Assert.That(_vm.Url, Is.Null);
    }

    [Test]
    public async Task Toggle_WhenStartThrows_StaysStoppedAndReportsError()
    {
        var throwing = new ThrowingController();
        var vm = new McpServerViewModel(throwing, port: 8731);

        await vm.ToggleCommand.ExecuteAsync(null);

        Assert.That(vm.IsRunning, Is.False);
        Assert.That(vm.StatusText, Is.EqualTo("port busy"));
    }

    private sealed class ThrowingController : IMcpServerController
    {
        public bool IsRunning => false;
        public string? Url => null;
        public Task StartAsync(int port) => throw new InvalidOperationException("port busy");
        public Task StopAsync() => Task.CompletedTask;
    }

    [Test]
    public async Task Toggle_OnSuccessfulStart_RaisesStarted()
    {
        var raised = false;
        _vm.Started += (_, _) => raised = true;

        await _vm.ToggleCommand.ExecuteAsync(null);

        Assert.That(raised, Is.True);
    }
}
