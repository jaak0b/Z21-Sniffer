using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Ports;

public interface IStationCurrentLimits
{
    int MaxCurrentMilliamps(StationHardware hardware);
}
