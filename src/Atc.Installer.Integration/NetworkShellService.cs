// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable IdentifierTypo
// ReSharper disable InvertIf
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable StringLiteralTypo
namespace Atc.Installer.Integration;

[SupportedOSPlatform("Windows")]
public class NetworkShellService : INetworkShellService
{
    private readonly FileInfo netshFile = new(@"C:\Windows\System32\netsh.exe");

    public async Task<IList<string>> GetUrlReservations()
    {
        var (isSuccessful, output) = await ProcessHelper
            .Execute(netshFile, "http show urlacl")
            .ConfigureAwait(true);

        if (!isSuccessful)
        {
            return new List<string>();
        }

        var list = new List<string>();
        foreach (var line in output
                     .ToLines()
                     .Where(x => x.Length > 0 && x.Contains("Reserved URL", StringComparison.Ordinal)))
        {
            var i = line.IndexOf(':', StringComparison.Ordinal) + 1;
            list.Add(line[i..].Trim());
        }

        return list;
    }

    public Task<(bool IsSucceeded, string? ErrorMessage)> AddUrlReservationEntryWithHttpPortForEveryone(
        ushort port)
        => ExecuteUrlReservationsCommand($"http add urlacl url=http://+:{port}/ user={GetTranslatedAccountNameForEveryone()}");

    public Task<(bool IsSucceeded, string? ErrorMessage)> AddUrlReservationEntryWithHttpPortForEveryone(
        string hostName,
        ushort port)
        => ExecuteUrlReservationsCommand($"http add urlacl url=http://{hostName}:{port}/ user={GetTranslatedAccountNameForEveryone()}");

    public Task<(bool IsSucceeded, string? ErrorMessage)> AddUrlReservationEntryWithHttpsPortForEveryone(
        string hostName,
        ushort port)
        => ExecuteUrlReservationsCommand($"http add urlacl url=https://{hostName}:{port}/ user={GetTranslatedAccountNameForEveryone()}");

    public Task<(bool IsSucceeded, string? ErrorMessage)> AddUrlReservationEntryWithHttpsPortForEveryone(
        ushort port)
        => ExecuteUrlReservationsCommand($"http add urlacl url=https://+:{port}/ user={GetTranslatedAccountNameForEveryone()}");

    public async Task<(bool IsSucceeded, string? ErrorMessage)> RemoveUrlReservationEntryByPort(
        ushort port)
    {
        var urlReservations = await GetUrlReservations()
            .ConfigureAwait(false);

        var urlReservation = urlReservations.FirstOrDefault(x => x.Contains($":{port}", StringComparison.Ordinal));
        if (urlReservation is null)
        {
            return (false, $"URL Reservation Entry don't exist by port={port}");
        }

        return await ExecuteUrlReservationsCommand($"http delete urlacl url={urlReservation}")
            .ConfigureAwait(false);
    }

    public async Task<(bool IsSucceeded, string? ErrorMessage)> RemoveUrlReservationEntryByHttpPort(
        ushort port)
    {
        var urlReservations = await GetUrlReservations()
            .ConfigureAwait(false);

        var urlReservation = urlReservations.FirstOrDefault(x => x.Contains("http:", StringComparison.OrdinalIgnoreCase) &&
                                                                 x.Contains($":{port}", StringComparison.Ordinal));
        if (urlReservation is null)
        {
            return (false, $"URL Reservation Entry don't exist by protocol=http, port={port}");
        }

        return await ExecuteUrlReservationsCommand($"http delete urlacl url={urlReservation}")
            .ConfigureAwait(false);
    }

    public async Task<(bool IsSucceeded, string? ErrorMessage)> RemoveUrlReservationEntryByHttpsPort(
        ushort port)
    {
        var urlReservations = await GetUrlReservations()
            .ConfigureAwait(false);

        var urlReservation = urlReservations.FirstOrDefault(x => x.Contains("https:", StringComparison.OrdinalIgnoreCase) &&
                                                                 x.Contains($":{port}", StringComparison.Ordinal));
        if (urlReservation is null)
        {
            return (false, $"URL Reservation Entry don't exist by protocol=https, port={port}");
        }

        return await ExecuteUrlReservationsCommand($"http delete urlacl url={urlReservation}")
            .ConfigureAwait(false);
    }

    private async Task<(bool IsSucceeded, string? ErrorMessage)> ExecuteUrlReservationsCommand(
        string command)
    {
        var (isSuccessful, output) = await ProcessHelper
            .Execute(netshFile, command)
            .ConfigureAwait(true);

        if (!isSuccessful)
        {
            return (false, output);
        }

        if (output.Contains("Error", StringComparison.OrdinalIgnoreCase))
        {
            var lines = output.ToLines();
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line) ||
                    line.Contains(":\\", StringComparison.Ordinal) ||
                    line.Contains("Microsoft", StringComparison.Ordinal))
                {
                    continue;
                }

                sb.AppendLine(line);
            }

            return (false, sb.ToString());
        }

        return (true, null);
    }

    private static string GetTranslatedAccountNameForEveryone()
    {
        var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null);

        var account = (NTAccount)sid.Translate(typeof(NTAccount));

        return account is null
            ? "Everyone"
            : account.Value;
    }
}