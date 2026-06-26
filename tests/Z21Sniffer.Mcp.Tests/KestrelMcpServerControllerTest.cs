using System.Net;
using System.Net.Sockets;
using FakeItEasy;
using NUnit.Framework;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Mcp;

namespace Z21Sniffer.Mcp.Tests;

[TestFixture]
public class KestrelMcpServerControllerTest
{
    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static bool CanConnect(int port)
    {
        try
        {
            using var client = new TcpClient();
            client.Connect(IPAddress.Loopback, port);
            return client.Connected;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    [Test]
    public async Task StartAsync_OpensLoopbackListener_StopAsync_ClosesIt()
    {
        var port = FreePort();
        var controller = new KestrelMcpServerController(() => A.Fake<ISnifferApi>());

        Assert.That(controller.IsRunning, Is.False, "no listener before StartAsync");
        Assert.That(CanConnect(port), Is.False, "port must be closed until the user starts the server");

        await controller.StartAsync(port);
        try
        {
            Assert.That(controller.IsRunning, Is.True);
            Assert.That(controller.Url, Does.Contain(port.ToString()));
            Assert.That(CanConnect(port), Is.True, "listener should be open after StartAsync");
        }
        finally
        {
            await controller.StopAsync();
        }

        Assert.That(controller.IsRunning, Is.False);
    }
}
