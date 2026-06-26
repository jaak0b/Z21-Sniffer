using CommunityToolkit.Mvvm.ComponentModel;
using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed partial class SensorRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _label;

    public SensorRowViewModel(SensorKey sensor, string label)
    {
        Sensor = sensor;
        _label = label;
    }

    public SensorKey Sensor { get; }
}
