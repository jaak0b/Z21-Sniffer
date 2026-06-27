using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed partial class RecordingViewModel : ObservableObject
{
    private readonly IIntervalSourceRegistry _registry;
    private readonly IRecordingClock _clock;
    private readonly Func<bool> _isConnected;
    private readonly Func<SystemSnapshot?> _currentSystem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleLabel))]
    [NotifyPropertyChangedFor(nameof(ShouldRecordFeedback))]
    private bool _isRecording;

    public RecordingViewModel(IIntervalSourceRegistry registry, IRecordingClock clock, Func<bool> isConnected, Func<SystemSnapshot?> currentSystem)
    {
        _registry = registry;
        _clock = clock;
        _isConnected = isConnected;
        _currentSystem = currentSystem;
        WeakReferenceMessenger.Default.Register<RecordingViewModel, LanguageChangedMessage>(
            this, (recipient, _) => recipient.OnPropertyChanged(nameof(ToggleLabel)));
    }

    public bool ShouldRecordFeedback => IsRecording;

    public string ToggleLabel => LocalizationService.Instance[IsRecording ? "StopRecording" : "StartRecording"];

    [RelayCommand]
    private void Toggle()
    {
        if (IsRecording) Stop();
        else Start();
    }

    private void Start()
    {
        _registry.Clear();
        _clock.Start();
        _registry.GetOrCreate<ConnectionSource>("connection").Set(_isConnected(), _clock.Now);

        var trackPower = _registry.GetOrCreate<TrackPowerSource>("trackpower");
        if (_currentSystem() is { } snapshot) trackPower.Apply(snapshot, _clock.Now);
        else trackPower.Set(TrackPowerStatus.Off, _clock.Now);

        IsRecording = true;
    }

    private void Stop()
    {
        _clock.Stop();
        foreach (var source in _registry.Sources)
            source.CloseOpenIntervals(_clock.Now, IntervalEndReason.Stopped);
        IsRecording = false;
    }
}
