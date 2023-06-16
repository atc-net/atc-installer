namespace Atc.Installer.Integration;

public interface INetworkShellService
{
    Task<bool> OpenHttpPortForEveryone(
        ushort port);

    Task<bool> OpenHttpsPortForEveryone(
        ushort port);
}