using System.ComponentModel;

namespace Z21Sniffer.Presentation.Controls;

public interface ISelectableOption : INotifyPropertyChanged
{
    string Label { get; }

    bool IsSelected { get; set; }
}
