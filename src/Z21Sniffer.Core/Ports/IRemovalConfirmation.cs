namespace Z21Sniffer.Core.Ports;

public interface IRemovalConfirmation
{
    Task<bool> ConfirmAsync();
}
