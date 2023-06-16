// ReSharper disable StringLiteralTypo
// ReSharper disable InvertIf
namespace Atc.Installer.Integration;

public class NetworkShellService : INetworkShellService
{
    [SupportedOSPlatform("Windows")]
    public Task<bool> OpenHttpPortForEveryone(
        ushort port)
        => OpenPort($"netsh http add urlacl url=http://+:{port}/ user=Everyone");

    [SupportedOSPlatform("Windows")]
    public Task<bool> OpenHttpsPortForEveryone(
        ushort port)
        => OpenPort($"netsh http add urlacl url=https://+:{port}/ user=Everyone");

    [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "OK - not possible for process.StandardInput.")]
    [SuppressMessage("Usage", "MA0004:Use Task.ConfigureAwait(false)", Justification = "OK - not possible for process.StandardInput.")]
    private static async Task<bool> OpenPort(
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

        return output.Contains("error", StringComparison.OrdinalIgnoreCase);
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
}