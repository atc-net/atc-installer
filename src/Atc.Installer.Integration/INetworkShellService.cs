namespace Atc.Installer.Integration;

public interface INetworkShellService
{
    Task<IList<string>> GetUrlReservations();

    Task<(bool IsSucceeded, string? ErrorMessage)> AddUrlReservationEntryWithHttpPortForEveryone(
        ushort port);

    Task<(bool IsSucceeded, string? ErrorMessage)> AddUrlReservationEntryWithHttpPortForEveryone(
        string hostName,
        ushort port);

    Task<(bool IsSucceeded, string? ErrorMessage)> AddUrlReservationEntryWithHttpsPortForEveryone(
        ushort port);

    Task<(bool IsSucceeded, string? ErrorMessage)> AddUrlReservationEntryWithHttpsPortForEveryone(
        string hostName,
        ushort port);

    Task<(bool IsSucceeded, string? ErrorMessage)> RemoveUrlReservationEntryByPort(
        ushort port);

    Task<(bool IsSucceeded, string? ErrorMessage)> RemoveUrlReservationEntryByHttpPort(
        ushort port);

    Task<(bool IsSucceeded, string? ErrorMessage)> RemoveUrlReservationEntryByHttpsPort(
        ushort port);
}