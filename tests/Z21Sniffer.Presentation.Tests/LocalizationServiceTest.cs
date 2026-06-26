using CommunityToolkit.Mvvm.Messaging;
using NUnit.Framework;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class LocalizationServiceTest
{
    [TearDown]
    public void ResetToEnglish() => LocalizationService.Instance.Apply("en");

    [Test]
    public void Indexer_DefaultEnglish_ReturnsEnglishText()
    {
        LocalizationService.Instance.Apply("en");

        Assert.That(LocalizationService.Instance["Connect"], Is.EqualTo("Connect"));
    }

    [Test]
    public void Apply_German_ReturnsGermanText()
    {
        LocalizationService.Instance.Apply("de");

        Assert.That(LocalizationService.Instance["Connect"], Is.EqualTo("Verbinden"));
    }

    [Test]
    public void RecordingKeys_English_ReturnEnglishText()
    {
        LocalizationService.Instance.Apply("en");

        Assert.That(LocalizationService.Instance["StartRecording"], Is.EqualTo("Start recording"));
        Assert.That(LocalizationService.Instance["StopRecording"], Is.EqualTo("Stop recording"));
    }

    [Test]
    public void RecordingKeys_German_ReturnGermanText()
    {
        LocalizationService.Instance.Apply("de");

        Assert.That(LocalizationService.Instance["StartRecording"], Is.EqualTo("Aufnahme starten"));
        Assert.That(LocalizationService.Instance["StopRecording"], Is.EqualTo("Aufnahme stoppen"));
    }

    [Test]
    public void ConnectionStateKeys_English_ReturnEnglishText()
    {
        LocalizationService.Instance.Apply("en");

        Assert.That(LocalizationService.Instance["Connected"], Is.EqualTo("Connected"));
        Assert.That(LocalizationService.Instance["Disconnected"], Is.EqualTo("Disconnected"));
    }

    [Test]
    public void ConnectionStateKeys_German_ReturnGermanText()
    {
        LocalizationService.Instance.Apply("de");

        Assert.That(LocalizationService.Instance["Connected"], Is.EqualTo("Verbunden"));
        Assert.That(LocalizationService.Instance["Disconnected"], Is.EqualTo("Getrennt"));
    }

    [Test]
    public void Indexer_UnknownKey_ReturnsKeyItself()
    {
        Assert.That(LocalizationService.Instance["NoSuchKey"], Is.EqualTo("NoSuchKey"));
    }

    [Test]
    public void Apply_RaisesPropertyChangedWithEmptyNameSoAllBindingsRefresh()
    {
        string? name = "unset";
        void Handler(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => name = e.PropertyName;
        LocalizationService.Instance.PropertyChanged += Handler;

        LocalizationService.Instance.Apply("de");
        LocalizationService.Instance.PropertyChanged -= Handler;

        Assert.That(name, Is.Empty);
    }

    [Test]
    public void Apply_RaisesLanguageChanged()
    {
        var raised = false;
        void Handler(object? sender, EventArgs e) => raised = true;
        LocalizationService.Instance.LanguageChanged += Handler;

        LocalizationService.Instance.Apply("de");
        LocalizationService.Instance.LanguageChanged -= Handler;

        Assert.That(raised, Is.True);
    }

    [Test]
    public void Apply_UpdatesCurrentCode()
    {
        LocalizationService.Instance.Apply("de");

        Assert.That(LocalizationService.Instance.CurrentCode, Is.EqualTo("de"));
    }

    [Test]
    public void Apply_BroadcastsLanguageChangedMessage()
    {
        var recipient = new object();
        var received = false;
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(recipient, (_, _) => received = true);
        try
        {
            LocalizationService.Instance.Apply("de");
        }
        finally
        {
            WeakReferenceMessenger.Default.Unregister<LanguageChangedMessage>(recipient);
        }

        Assert.That(received, Is.True);
    }
}
