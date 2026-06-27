using FakeItEasy;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class ConnectionViewModelTest
{
    private ICommandStationConnectionFactory _factory = null!;
    private ISettingsStore _settings = null!;
    private ICommandStationConnection _connection = null!;
    private ConnectionViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = A.Fake<ICommandStationConnectionFactory>();
        _settings = A.Fake<ISettingsStore>();
        _connection = A.Fake<ICommandStationConnection>();
        A.CallTo(() => _settings.Load()).Returns(new AppSettings("192.168.0.5", 21105, "en"));
        A.CallTo(() => _factory.Create(A<bool>._)).Returns(_connection);
        _vm = new ConnectionViewModel(_factory, _settings);
    }

    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    [Test]
    public void Constructor_LoadsHostAndPortFromSettings()
    {
        Assert.That(_vm.Host, Is.EqualTo("192.168.0.5"));
        Assert.That(_vm.Port, Is.EqualTo(21105));
    }

    [Test]
    public void Source_DefaultsToZ21()
    {
        Assert.That(_vm.Source, Is.EqualTo(ConnectionSourceType.Z21));
        Assert.That(_vm.IsSimulated, Is.False);
        Assert.That(_vm.ShowZ21Settings, Is.True);
    }

    [Test]
    public void Source_Simulation_SetsIsSimulatedAndHidesZ21Settings()
    {
        _vm.Source = ConnectionSourceType.Simulation;

        Assert.That(_vm.IsSimulated, Is.True);
        Assert.That(_vm.ShowZ21Settings, Is.False);
    }

    [Test]
    public void Source_Z21_ClearsIsSimulatedAndShowsZ21Settings()
    {
        _vm.Source = ConnectionSourceType.Simulation;
        _vm.Source = ConnectionSourceType.Z21;

        Assert.That(_vm.IsSimulated, Is.False);
        Assert.That(_vm.ShowZ21Settings, Is.True);
    }

    [Test]
    public async Task ToggleConnection_WhenDisconnectedAndSimulated_CreatesSimulatedConnectionAndConnects()
    {
        _vm.Source = ConnectionSourceType.Simulation;
        _vm.Host = "10.0.0.9";
        _vm.Port = 21106;

        await _vm.ToggleConnectionCommand.ExecuteAsync(null);

        A.CallTo(() => _factory.Create(true)).MustHaveHappened();
        A.CallTo(() => _connection.ConnectAsync("10.0.0.9", 21106)).MustHaveHappened();
    }

    [Test]
    public async Task ToggleConnection_WhenDisconnected_RaisesConnectionActivated()
    {
        ICommandStationConnection? activated = null;
        _vm.ConnectionActivated += c => activated = c;

        await _vm.ToggleConnectionCommand.ExecuteAsync(null);

        Assert.That(activated, Is.SameAs(_connection));
    }

    [Test]
    public async Task ToggleConnection_WhenDisconnected_PersistsHostAndPort()
    {
        _vm.Host = "10.0.0.9";
        _vm.Port = 21106;

        await _vm.ToggleConnectionCommand.ExecuteAsync(null);

        A.CallTo(() => _settings.Save(A<AppSettings>.That.Matches(s => s.Host == "10.0.0.9" && s.Port == 21106)))
            .MustHaveHappened();
    }

    [Test]
    public async Task ToggleConnection_WhenConnected_DisconnectsActiveConnection()
    {
        await _vm.ToggleConnectionCommand.ExecuteAsync(null);
        _vm.IsConnected = true;

        await _vm.ToggleConnectionCommand.ExecuteAsync(null);

        A.CallTo(() => _connection.DisconnectAsync()).MustHaveHappened();
    }

    [Test]
    public async Task SetTrackPowerAsync_AfterConnect_DelegatesToActiveConnection()
    {
        await _vm.ToggleConnectionCommand.ExecuteAsync(null);

        await _vm.SetTrackPowerAsync(true);

        A.CallTo(() => _connection.SetTrackPowerAsync(true)).MustHaveHappened();
    }

    [Test]
    public void SetTrackPowerAsync_BeforeConnect_ThrowsInvalidOperation()
    {
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => _vm.SetTrackPowerAsync(true));

        Assert.That(exception!.Message, Is.EqualTo("Not connected to a command station."));
    }

    [Test]
    public async Task RequestCurrentStateAsync_WhenConnected_DelegatesToActiveConnection()
    {
        A.CallTo(() => _connection.IsConnected).Returns(true);
        await _vm.ToggleConnectionCommand.ExecuteAsync(null);

        await _vm.RequestCurrentStateAsync();

        A.CallTo(() => _connection.RequestCurrentStateAsync()).MustHaveHappened();
    }

    [Test]
    public async Task RequestCurrentStateAsync_BeforeConnect_DoesNothing()
    {
        await _vm.RequestCurrentStateAsync();

        A.CallTo(() => _connection.RequestCurrentStateAsync()).MustNotHaveHappened();
    }

    [Test]
    public async Task RequestCurrentStateAsync_WhenActiveButNotConnected_DoesNothing()
    {
        await _vm.ToggleConnectionCommand.ExecuteAsync(null);

        await _vm.RequestCurrentStateAsync();

        A.CallTo(() => _connection.RequestCurrentStateAsync()).MustNotHaveHappened();
    }

    [Test]
    public void ToggleLabel_WhenDisconnected_IsLocalizedConnect()
    {
        Assert.That(_vm.ToggleLabel, Is.EqualTo(LocalizationService.Instance["Connect"]));
    }

    [Test]
    public void ToggleLabel_WhenConnected_IsLocalizedDisconnect()
    {
        _vm.IsConnected = true;

        Assert.That(_vm.ToggleLabel, Is.EqualTo(LocalizationService.Instance["Disconnect"]));
    }

    [Test]
    public void ToggleLabel_WhenConnectionChanges_RaisesPropertyChanged()
    {
        var raised = false;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ConnectionViewModel.ToggleLabel)) raised = true;
        };

        _vm.IsConnected = true;

        Assert.That(raised, Is.True);
    }

    [Test]
    public void ToggleLabel_WhenLanguageChanges_RaisesPropertyChanged()
    {
        var raised = false;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ConnectionViewModel.ToggleLabel)) raised = true;
        };

        LocalizationService.Instance.Apply("de");

        Assert.That(raised, Is.True);
    }

    [Test]
    public void CanEditConnection_TrueWhenDisconnected()
    {
        Assert.That(_vm.CanEditConnection, Is.True);
    }

    [Test]
    public void CanEditConnection_FalseWhenConnected()
    {
        _vm.IsConnected = true;

        Assert.That(_vm.CanEditConnection, Is.False);
    }
}
