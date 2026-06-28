using FakeItEasy;
using NUnit.Framework;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Presentation.Updates;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class UpdateCheckTest
{
    private IAppUpdater _updater = null!;
    private IAppLog _log = null!;
    private UpdateCheck _check = null!;

    [SetUp]
    public void SetUp()
    {
        _updater = A.Fake<IAppUpdater>();
        _log = A.Fake<IAppLog>();
        _check = new UpdateCheck(_updater, _log);
    }

    [Test]
    public async Task RunAsync_WhenUpdateAvailable_DownloadsThenAppliesOnExit()
    {
        A.CallTo(() => _updater.CheckForUpdateAsync()).Returns(true);

        await _check.RunAsync();

        A.CallTo(() => _updater.DownloadUpdateAsync()).MustHaveHappened()
            .Then(A.CallTo(() => _updater.ApplyUpdateOnExit()).MustHaveHappened());
    }

    [Test]
    public async Task RunAsync_WhenNoUpdateAvailable_DoesNotDownloadOrApply()
    {
        A.CallTo(() => _updater.CheckForUpdateAsync()).Returns(false);

        await _check.RunAsync();

        A.CallTo(() => _updater.DownloadUpdateAsync()).MustNotHaveHappened();
        A.CallTo(() => _updater.ApplyUpdateOnExit()).MustNotHaveHappened();
    }

    [Test]
    public async Task RunAsync_WhenCheckThrows_SwallowsAndLogs()
    {
        var failure = new InvalidOperationException("update server unreachable");
        A.CallTo(() => _updater.CheckForUpdateAsync()).Throws(failure);

        await _check.RunAsync();

        A.CallTo(() => _updater.DownloadUpdateAsync()).MustNotHaveHappened();
        A.CallTo(() => _log.Error(failure, A<string>.That.Contains("update"))).MustHaveHappened();
    }
}
