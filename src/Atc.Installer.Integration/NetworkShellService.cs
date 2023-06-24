// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable InvertIf
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable StringLiteralTypo
namespace Atc.Installer.Integration;

[SupportedOSPlatform("Windows")]
public class NetworkShellService : INetworkShellService
{
    public async Task<IList<string>> GetUrlReservations()
    {
        var output = await ExecuteCmdCommandAndReadStandardOutput("netsh http show urlacl")
            .ConfigureAwait(false);

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
        => ExecuteUrlReservationsCommand($"netsh http add urlacl url=http://+:{port}/ user={GetTranslatedAccountNameForEveryone()}");

    public Task<(bool IsSucceeded, string? ErrorMessage)> AddUrlReservationEntryWithHttpPortForEveryone(
        string hostName,
        ushort port)
        => ExecuteUrlReservationsCommand($"netsh http add urlacl url=http://{hostName}:{port}/ user={GetTranslatedAccountNameForEveryone()}");

    public Task<(bool IsSucceeded, string? ErrorMessage)> AddUrlReservationEntryWithHttpsPortForEveryone(
        string hostName,
        ushort port)
        => ExecuteUrlReservationsCommand($"netsh http add urlacl url=https://{hostName}:{port}/ user={GetTranslatedAccountNameForEveryone()}");

    public Task<(bool IsSucceeded, string? ErrorMessage)> AddUrlReservationEntryWithHttpsPortForEveryone(
        ushort port)
        => ExecuteUrlReservationsCommand($"netsh http add urlacl url=https://+:{port}/ user={GetTranslatedAccountNameForEveryone()}");

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

        return await ExecuteUrlReservationsCommand($"netsh http delete urlacl url={urlReservation}")
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

        return await ExecuteUrlReservationsCommand($"netsh http delete urlacl url={urlReservation}")
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

        return await ExecuteUrlReservationsCommand($"netsh http delete urlacl url={urlReservation}")
            .ConfigureAwait(false);
    }

    private static async Task<(bool IsSucceeded, string? ErrorMessage)> ExecuteUrlReservationsCommand(
        string command)
    {
        var output = await ExecuteCmdCommandAndReadStandardOutput(command)
            .ConfigureAwait(false);

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

    private static FileInfo GetCmdFile()
    {
        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
        if (systemRoot is not null)
        {
            var cmdPath = Path.Combine(systemRoot, "System32", "cmd.exe");
            if (File.Exists(cmdPath))
            {
                return new FileInfo(cmdPath);
            }
        }

        throw new FileNotFoundException("Could not find cmd.exe");
    }

    [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "OK.")]
    [SuppressMessage("Usage", "MA0004:Use Task.ConfigureAwait(false)", Justification = "OK - not possible for process.StandardInput.")]
    private static async Task<string> ExecuteCmdCommandAndReadStandardOutput(
        string command)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = GetCmdFile().FullName,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var process = new Process
        {
            StartInfo = processStartInfo,
        };

        process.Start();

        await using (var streamWriter = process.StandardInput)
        {
            if (streamWriter.BaseStream.CanWrite)
            {
                await streamWriter
                    .WriteLineAsync(command)
                    .ConfigureAwait(false);
                await streamWriter
                    .WriteLineAsync("exit")
                    .ConfigureAwait(false);
            }
        }

        var output = await process.StandardOutput
            .ReadToEndAsync()
            .ConfigureAwait(false);

        await process
            .WaitForExitAsync()
            .ConfigureAwait(false);

        process.Dispose();

        return output;
    }
}